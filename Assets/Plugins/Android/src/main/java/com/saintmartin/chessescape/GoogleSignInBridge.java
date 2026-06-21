package com.saintmartin.chessescape;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

import com.google.android.gms.auth.api.signin.GoogleSignIn;
import com.google.android.gms.auth.api.signin.GoogleSignInAccount;
import com.google.android.gms.auth.api.signin.GoogleSignInClient;
import com.google.android.gms.auth.api.signin.GoogleSignInOptions;
import com.google.android.gms.common.api.ApiException;
import com.google.android.gms.tasks.OnCompleteListener;
import com.google.android.gms.tasks.Task;
import com.google.firebase.auth.AuthCredential;
import com.google.firebase.auth.FirebaseAuth;
import com.google.firebase.auth.FirebaseUser;
import com.google.firebase.auth.GoogleAuthProvider;

import org.json.JSONException;
import org.json.JSONObject;

public class GoogleSignInBridge extends Activity {
    private static final int RC_SIGN_IN = 9001;
    private static String gameObjectName;
    private static String callbackMethod;
    private static String requestId;
    private static String webClientId;
    private GoogleSignInClient signInClient;

    public static void signIn(String unityObjectName, String requestIdValue, String clientId) {
        Activity unityActivity = com.unity3d.player.UnityPlayer.currentActivity;
        if (unityActivity == null) {
            sendUnityError(unityObjectName, requestIdValue, "onFirebaseBridgeResult", "No hay actividad de Unity disponible.");
            return;
        }

        gameObjectName = unityObjectName;
        callbackMethod = "OnFirebaseBridgeResult";
        requestId = requestIdValue;
        webClientId = clientId;

        Intent intent = new Intent(unityActivity, GoogleSignInBridge.class);
        unityActivity.startActivity(intent);
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        if (webClientId == null || webClientId.isEmpty()) {
            sendUnityError(gameObjectName, requestId, callbackMethod, "No se entregó el webClientId para Google Sign-In.");
            finish();
            return;
        }

        GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DEFAULT_SIGN_IN)
                .requestIdToken(webClientId)
                .requestEmail()
                .build();

        signInClient = GoogleSignIn.getClient(this, gso);
        Intent signInIntent = signInClient.getSignInIntent();
        startActivityForResult(signInIntent, RC_SIGN_IN);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);

        if (requestCode != RC_SIGN_IN) {
            finish();
            return;
        }

        Task<GoogleSignInAccount> task = GoogleSignIn.getSignedInAccountFromIntent(data);
        try {
            GoogleSignInAccount account = task.getResult(ApiException.class);
            if (account == null) {
                sendUnityError(gameObjectName, requestId, callbackMethod, "No se obtuvo cuenta de Google.");
                finish();
                return;
            }

            String idToken = account.getIdToken();
            if (idToken == null || idToken.isEmpty()) {
                sendUnityError(gameObjectName, requestId, callbackMethod, "No se obtuvo idToken de Google.");
                finish();
                return;
            }

            FirebaseAuth auth = FirebaseAuth.getInstance();
            AuthCredential credential = GoogleAuthProvider.getCredential(idToken, null);
            auth.signInWithCredential(credential).addOnCompleteListener(this, new OnCompleteListener<com.google.firebase.auth.AuthResult>() {
                @Override
                public void onComplete(Task<com.google.firebase.auth.AuthResult> signInTask) {
                    if (signInTask.isSuccessful()) {
                        FirebaseUser user = auth.getCurrentUser();
                        sendUnitySuccess(user);
                    } else {
                        Exception exception = signInTask.getException();
                        sendUnityError(gameObjectName, requestId, callbackMethod, exception != null ? exception.getMessage() : "Error desconocido en Firebase sign-in.");
                    }
                    finish();
                }
            });
        } catch (ApiException e) {
            sendUnityError(gameObjectName, requestId, callbackMethod, "Google Sign-In falló: " + e.getMessage());
            finish();
        }
    }

    private static void sendUnitySuccess(FirebaseUser user) {
        try {
            JSONObject payload = new JSONObject();
            payload.put("signedIn", true);
            payload.put("uid", user != null ? user.getUid() : "");
            payload.put("email", user != null && user.getEmail() != null ? user.getEmail() : "");
            payload.put("displayName", user != null && user.getDisplayName() != null ? user.getDisplayName() : "");
            payload.put("photoURL", user != null && user.getPhotoUrl() != null ? user.getPhotoUrl().toString() : "");
            payload.put("provider", "google");

            JSONObject response = new JSONObject();
            response.put("requestId", requestId != null ? requestId : "");
            response.put("success", true);
            response.put("action", "signIn");
            response.put("payload", payload.toString());
            response.put("error", "");

            com.unity3d.player.UnityPlayer.UnitySendMessage(gameObjectName, callbackMethod, response.toString());
        } catch (JSONException ex) {
            sendUnityError(gameObjectName, requestId, callbackMethod, "Error construyendo la respuesta JSON: " + ex.getMessage());
        }
    }

    private static void sendUnityError(String gameObject, String requestIdValue, String callback, String errorMessage) {
        try {
            JSONObject response = new JSONObject();
            response.put("requestId", requestIdValue != null ? requestIdValue : "");
            response.put("success", false);
            response.put("action", "signIn");
            response.put("payload", JSONObject.NULL);
            response.put("error", errorMessage != null ? errorMessage : "Error desconocido");
            com.unity3d.player.UnityPlayer.UnitySendMessage(gameObject, callback, response.toString());
        } catch (JSONException ex) {
            Log.e("GoogleSignInBridge", "No se pudo enviar el error a Unity: " + ex.getMessage());
        }
    }
}
