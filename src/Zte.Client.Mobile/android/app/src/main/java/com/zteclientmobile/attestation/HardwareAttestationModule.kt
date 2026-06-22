package com.zteclientmobile.attestation

import android.app.ActivityManager
import android.content.Context
import android.os.Build
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import com.facebook.react.bridge.Arguments
import com.facebook.react.bridge.Promise
import com.facebook.react.bridge.ReactApplicationContext
import com.facebook.react.bridge.ReactContextBaseJavaModule
import com.facebook.react.bridge.ReactMethod
import java.security.KeyPairGenerator
import java.security.KeyStore
import java.security.MessageDigest
import java.security.Signature
import java.security.spec.ECGenParameterSpec
import java.util.Locale

class HardwareAttestationModule(
    reactContext: ReactApplicationContext
) : ReactContextBaseJavaModule(reactContext) {

    override fun getName(): String = "HardwareAttestation"

    @ReactMethod
    fun generateKeyWithAttestation(
        alias: String,
        challengeBase64: String,
        promise: Promise
    ) {
        try {
            val challenge = Base64.decode(challengeBase64, Base64.NO_WRAP)

            val keyPairGenerator = KeyPairGenerator.getInstance(
                KeyProperties.KEY_ALGORITHM_EC,
                ANDROID_KEYSTORE
            )

            val keySpec = KeyGenParameterSpec.Builder(
                alias,
                KeyProperties.PURPOSE_SIGN or KeyProperties.PURPOSE_VERIFY
            )
                .setAlgorithmParameterSpec(ECGenParameterSpec("secp256r1"))
                .setDigests(KeyProperties.DIGEST_SHA256)
                .setAttestationChallenge(challenge)
                .setUserAuthenticationRequired(false)
                .build()

            keyPairGenerator.initialize(keySpec)
            val keyPair = keyPairGenerator.generateKeyPair()

            val keyStore = KeyStore.getInstance(ANDROID_KEYSTORE)
            keyStore.load(null)

            val certificateChain = keyStore.getCertificateChain(alias)

            val certificates = Arguments.createArray()

            certificateChain?.forEach { certificate ->
                val certBase64 = Base64.encodeToString(
                    certificate.encoded,
                    Base64.NO_WRAP
                )

                certificates.pushString(certBase64)
            }

            val publicKeyBase64 = Base64.encodeToString(
                keyPair.public.encoded,
                Base64.NO_WRAP
            )

            val result = Arguments.createMap()
            result.putString("alias", alias)
            result.putString("publicKeyBase64", publicKeyBase64)
            result.putArray("certificateChainBase64", certificates)

            promise.resolve(result)
        } catch (ex: Exception) {
            promise.reject(
                "HARDWARE_ATTESTATION_KEY_GENERATION_FAILED",
                ex.message,
                ex
            )
        }
    }

    @ReactMethod
    fun signChallenge(
        alias: String,
        challengeBase64: String,
        promise: Promise
    ) {
        try {
            val challenge = Base64.decode(challengeBase64, Base64.NO_WRAP)

            val keyStore = KeyStore.getInstance(ANDROID_KEYSTORE)
            keyStore.load(null)

            val privateKey = keyStore.getKey(alias, null)

            if (privateKey == null) {
                promise.reject(
                    "HARDWARE_ATTESTATION_PRIVATE_KEY_NOT_FOUND",
                    "Private key was not found for alias: $alias"
                )
                return
            }

            val signature = Signature.getInstance("SHA256withECDSA")
            signature.initSign(privateKey as java.security.PrivateKey)
            signature.update(challenge)

            val signatureBase64 = Base64.encodeToString(
                signature.sign(),
                Base64.NO_WRAP
            )

            val result = Arguments.createMap()
            result.putString("alias", alias)
            result.putString("signatureBase64", signatureBase64)

            promise.resolve(result)
        } catch (ex: Exception) {
            promise.reject(
                "HARDWARE_ATTESTATION_SIGN_FAILED",
                ex.message,
                ex
            )
        }
    }

    @ReactMethod
    fun deleteKey(
        alias: String,
        promise: Promise
    ) {
        try {
            val keyStore = KeyStore.getInstance(ANDROID_KEYSTORE)
            keyStore.load(null)

            if (keyStore.containsAlias(alias)) {
                keyStore.deleteEntry(alias)
            }

            promise.resolve(true)
        } catch (ex: Exception) {
            promise.reject(
                "HARDWARE_ATTESTATION_DELETE_KEY_FAILED",
                ex.message,
                ex
            )
        }
    }


    @ReactMethod
    fun getDeviceIdentity(promise: Promise) {
        try {
            val rawIdentity = listOf(
                Build.MANUFACTURER ?: "",
                Build.BRAND ?: "",
                Build.MODEL ?: "",
                Build.DEVICE ?: "",
                Build.PRODUCT ?: "",
                Build.HARDWARE ?: "",
                Build.FINGERPRINT ?: ""
            ).joinToString(separator = "|")

            val deviceId = sha256(rawIdentity).take(32)

            val supportedAbis = Arguments.createArray()
            Build.SUPPORTED_ABIS.forEach { abi ->
                supportedAbis.pushString(abi)
            }

            val result = Arguments.createMap()
            result.putString("deviceId", deviceId)
            result.putString("deviceName", "${Build.MANUFACTURER} ${Build.MODEL}".trim())
            result.putString("manufacturer", Build.MANUFACTURER ?: "")
            result.putString("brand", Build.BRAND ?: "")
            result.putString("model", Build.MODEL ?: "")
            result.putString("device", Build.DEVICE ?: "")
            result.putString("product", Build.PRODUCT ?: "")
            result.putString("hardware", Build.HARDWARE ?: "")
            result.putString("androidVersion", Build.VERSION.RELEASE ?: "")
            result.putInt("sdkInt", Build.VERSION.SDK_INT)
            result.putArray("supportedAbis", supportedAbis)
            result.putBoolean("isEmulator", isProbablyEmulator())
            result.putInt("cpuCoreCount", Runtime.getRuntime().availableProcessors())
            result.putDouble("totalMemoryBytes", getTotalMemoryBytes().toDouble())

            promise.resolve(result)
        } catch (ex: Exception) {
            promise.reject(
                "DEVICE_IDENTITY_FAILED",
                ex.message,
                ex
            )
        }
    }

    private fun sha256(value: String): String {
        val digest = MessageDigest.getInstance("SHA-256")
            .digest(value.toByteArray(Charsets.UTF_8))

        return digest.joinToString("") { byte ->
            "%02x".format(byte)
        }
    }

    private fun isProbablyEmulator(): Boolean {
        val fingerprint = Build.FINGERPRINT.lowercase(Locale.US)
        val model = Build.MODEL.lowercase(Locale.US)
        val manufacturer = Build.MANUFACTURER.lowercase(Locale.US)
        val brand = Build.BRAND.lowercase(Locale.US)
        val device = Build.DEVICE.lowercase(Locale.US)
        val product = Build.PRODUCT.lowercase(Locale.US)
        val hardware = Build.HARDWARE.lowercase(Locale.US)

        return fingerprint.startsWith("generic") ||
            fingerprint.contains("emulator") ||
            model.contains("google_sdk") ||
            model.contains("emulator") ||
            model.contains("android sdk built for") ||
            manufacturer.contains("genymotion") ||
            brand.startsWith("generic") ||
            device.startsWith("generic") ||
            product.contains("sdk") ||
            hardware.contains("goldfish") ||
            hardware.contains("ranchu")
    }

    private fun getTotalMemoryBytes(): Long {
        val activityManager = reactApplicationContext.getSystemService(
            Context.ACTIVITY_SERVICE
        ) as ActivityManager
        val memoryInfo = ActivityManager.MemoryInfo()
        activityManager.getMemoryInfo(memoryInfo)

        return memoryInfo.totalMem
    }

    companion object {
        private const val ANDROID_KEYSTORE = "AndroidKeyStore"
    }
}
