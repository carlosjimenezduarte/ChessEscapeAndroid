mergeInto(LibraryManager.library, {
  $ChessEscapeFirebaseBridge: {
    unityObjectName: "FirebaseWebBridge",
    firebaseReadyPromise: null,
    app: null,
    auth: null,
    db: null,

    firebaseConfig: {
      apiKey: "AIzaSyDhQZQT3XWxTHLHELT7JW0p5uKr1fwAO-w",
      authDomain: "www.chessescape.com",
      projectId: "chessescapeauth",
      storageBucket: "chessescapeauth.firebasestorage.app",
      messagingSenderId: "702777328635",
      appId: "1:702777328635:web:0525c318202f87d159815e",
      measurementId: "G-F3TNDFFXLN"
    },

    loadScript: function (src) {
      return new Promise(function (resolve, reject) {
        var existing = document.querySelector('script[src="' + src + '"]');
        if (existing) {
          if (existing.dataset.loaded === "true") {
            resolve();
            return;
          }

          existing.addEventListener("load", resolve);
          existing.addEventListener("error", function () {
            reject(new Error("No se pudo cargar " + src));
          });
          return;
        }

        var script = document.createElement("script");
        script.src = src;
        script.async = true;
        script.onload = function () {
          script.dataset.loaded = "true";
          resolve();
        };
        script.onerror = function () {
          reject(new Error("No se pudo cargar " + src));
        };
        document.head.appendChild(script);
      });
    },

    ensureFirebase: function () {
      var bridge = ChessEscapeFirebaseBridge;
      if (bridge.firebaseReadyPromise) return bridge.firebaseReadyPromise;

      bridge.firebaseReadyPromise = Promise.resolve()
        .then(function () {
          if (typeof firebase !== "undefined" && firebase.apps) return;

          var base = "https://www.gstatic.com/firebasejs/10.12.5/";
          return bridge.loadScript(base + "firebase-app-compat.js")
            .then(function () { return bridge.loadScript(base + "firebase-auth-compat.js"); })
            .then(function () { return bridge.loadScript(base + "firebase-firestore-compat.js"); });
        })
        .then(function () {
          bridge.app = firebase.apps.length ? firebase.app() : firebase.initializeApp(bridge.firebaseConfig);
          bridge.auth = firebase.auth();
          bridge.db = firebase.firestore();
          return bridge.auth.setPersistence(firebase.auth.Auth.Persistence.LOCAL);
        });

      return bridge.firebaseReadyPromise;
    },

    send: function (requestId, success, action, payload, error) {
      var response = {
        requestId: requestId || "",
        success: !!success,
        action: action || "",
        payload: payload === undefined || payload === null ? "" : JSON.stringify(payload),
        error: error ? String(error.message || error) : ""
      };

      SendMessage(
        ChessEscapeFirebaseBridge.unityObjectName,
        "OnFirebaseBridgeResult",
        JSON.stringify(response)
      );
    },

    userPayload: function (user) {
      if (!user) {
        return { signedIn: false };
      }

      return {
        signedIn: true,
        uid: user.uid || "",
        email: user.email || "",
        displayName: user.displayName || "",
        photoURL: user.photoURL || ""
      };
    },

    currentUserOrThrow: function () {
      var user = ChessEscapeFirebaseBridge.auth && ChessEscapeFirebaseBridge.auth.currentUser;
      if (!user) throw new Error("No hay usuario autenticado en Firebase.");
      return user;
    },

    userDocument: function (user) {
      return ChessEscapeFirebaseBridge.db.collection("users").doc(user.uid);
    },

    initialize: function (gameObjectName, requestId) {
      var bridge = ChessEscapeFirebaseBridge;
      bridge.unityObjectName = gameObjectName || bridge.unityObjectName;
      bridge.ensureFirebase()
        .then(function () {
          bridge.send(requestId, true, "initialize", bridge.userPayload(bridge.auth.currentUser));
        })
        .catch(function (error) {
          bridge.send(requestId, false, "initialize", null, error);
        });
    },

    getCurrentUser: function (requestId) {
      var bridge = ChessEscapeFirebaseBridge;
      bridge.ensureFirebase()
        .then(function () {
          bridge.send(requestId, true, "currentUser", bridge.userPayload(bridge.auth.currentUser));
        })
        .catch(function (error) {
          bridge.send(requestId, false, "currentUser", null, error);
        });
    },

    signInWithGoogle: function (requestId) {
      var bridge = ChessEscapeFirebaseBridge;
      bridge.ensureFirebase()
        .then(function () {
          var provider = new firebase.auth.GoogleAuthProvider();
          provider.setCustomParameters({ prompt: "select_account" });
          return bridge.auth.signInWithPopup(provider);
        })
        .then(function (result) {
          bridge.send(requestId, true, "signIn", bridge.userPayload(result.user));
        })
        .catch(function (error) {
          bridge.send(requestId, false, "signIn", null, error);
        });
    },

    signOut: function (requestId) {
      var bridge = ChessEscapeFirebaseBridge;
      bridge.ensureFirebase()
        .then(function () { return bridge.auth.signOut(); })
        .then(function () {
          bridge.send(requestId, true, "signOut", { signedIn: false });
        })
        .catch(function (error) {
          bridge.send(requestId, false, "signOut", null, error);
        });
    },

    loadUserProfile: function (requestId) {
      var bridge = ChessEscapeFirebaseBridge;
      bridge.ensureFirebase()
        .then(function () {
          var user = bridge.currentUserOrThrow();
          return bridge.userDocument(user).get().then(function (doc) {
            var data = doc.exists ? doc.data() : {};
            bridge.send(requestId, true, "loadUserProfile", Object.assign(bridge.userPayload(user), {
              exists: doc.exists,
              username: data.username || "",
              avatarId: data.avatarId || "",
              joined_at: data.joined_at || "",
              userNumber: data.userNumber || 0,
              provider: data.provider || "google"
            }));
          });
        })
        .catch(function (error) {
          bridge.send(requestId, false, "loadUserProfile", null, error);
        });
    },

    saveUserProfile: function (requestId, json) {
      var bridge = ChessEscapeFirebaseBridge;
      bridge.ensureFirebase()
        .then(function () {
          var user = bridge.currentUserOrThrow();
          var profile = JSON.parse(json || "{}");
          var data = {
            uid: user.uid,
            email: user.email || "",
            displayName: user.displayName || "",
            photoURL: user.photoURL || "",
            provider: "google",
            username: profile.username || "",
            avatarId: profile.avatarId || "",
            joined_at: profile.joined_at || new Date().toISOString(),
            userNumber: profile.userNumber || Math.floor(Date.now() / 1000),
            updated_at: new Date().toISOString()
          };

          return bridge.userDocument(user).set(data, { merge: true }).then(function () {
            bridge.send(requestId, true, "saveUserProfile", Object.assign(bridge.userPayload(user), data, { exists: true }));
          });
        })
        .catch(function (error) {
          bridge.send(requestId, false, "saveUserProfile", null, error);
        });
    },

    loadSlot: function (requestId, slotId) {
      var bridge = ChessEscapeFirebaseBridge;
      bridge.ensureFirebase()
        .then(function () {
          var user = bridge.currentUserOrThrow();
          return bridge.userDocument(user).collection("slots").doc(slotId).get().then(function (doc) {
            var data = doc.exists ? doc.data() : {};
            bridge.send(requestId, true, "loadSlot", {
              exists: doc.exists,
              slotId: slotId,
              json: data.json || ""
            });
          });
        })
        .catch(function (error) {
          bridge.send(requestId, false, "loadSlot", null, error);
        });
    },

    saveSlot: function (requestId, slotId, json) {
      var bridge = ChessEscapeFirebaseBridge;
      bridge.ensureFirebase()
        .then(function () {
          var user = bridge.currentUserOrThrow();
          return bridge.userDocument(user).collection("slots").doc(slotId).set({
            slotId: slotId,
            json: json || "",
            updated_at: new Date().toISOString()
          }, { merge: true });
        })
        .then(function () {
          bridge.send(requestId, true, "saveSlot", { slotId: slotId });
        })
        .catch(function (error) {
          bridge.send(requestId, false, "saveSlot", null, error);
        });
    }
  },

  FirebaseBridge_Initialize__deps: ["$ChessEscapeFirebaseBridge"],
  FirebaseBridge_Initialize: function (gameObjectPtr, requestIdPtr) {
    ChessEscapeFirebaseBridge.initialize(
      UTF8ToString(gameObjectPtr),
      UTF8ToString(requestIdPtr)
    );
  },

  FirebaseBridge_GetCurrentUser__deps: ["$ChessEscapeFirebaseBridge"],
  FirebaseBridge_GetCurrentUser: function (requestIdPtr) {
    ChessEscapeFirebaseBridge.getCurrentUser(UTF8ToString(requestIdPtr));
  },

  FirebaseBridge_SignInWithGoogle__deps: ["$ChessEscapeFirebaseBridge"],
  FirebaseBridge_SignInWithGoogle: function (requestIdPtr) {
    ChessEscapeFirebaseBridge.signInWithGoogle(UTF8ToString(requestIdPtr));
  },

  FirebaseBridge_SignOut__deps: ["$ChessEscapeFirebaseBridge"],
  FirebaseBridge_SignOut: function (requestIdPtr) {
    ChessEscapeFirebaseBridge.signOut(UTF8ToString(requestIdPtr));
  },

  FirebaseBridge_LoadUserProfile__deps: ["$ChessEscapeFirebaseBridge"],
  FirebaseBridge_LoadUserProfile: function (requestIdPtr) {
    ChessEscapeFirebaseBridge.loadUserProfile(UTF8ToString(requestIdPtr));
  },

  FirebaseBridge_SaveUserProfile__deps: ["$ChessEscapeFirebaseBridge"],
  FirebaseBridge_SaveUserProfile: function (requestIdPtr, jsonPtr) {
    ChessEscapeFirebaseBridge.saveUserProfile(
      UTF8ToString(requestIdPtr),
      UTF8ToString(jsonPtr)
    );
  },

  FirebaseBridge_LoadSlot__deps: ["$ChessEscapeFirebaseBridge"],
  FirebaseBridge_LoadSlot: function (requestIdPtr, slotIdPtr) {
    ChessEscapeFirebaseBridge.loadSlot(
      UTF8ToString(requestIdPtr),
      UTF8ToString(slotIdPtr)
    );
  },

  FirebaseBridge_SaveSlot__deps: ["$ChessEscapeFirebaseBridge"],
  FirebaseBridge_SaveSlot: function (requestIdPtr, slotIdPtr, jsonPtr) {
    ChessEscapeFirebaseBridge.saveSlot(
      UTF8ToString(requestIdPtr),
      UTF8ToString(slotIdPtr),
      UTF8ToString(jsonPtr)
    );
  }
});
