import { NativeModules, Platform } from 'react-native';

const { HardwareAttestation } = NativeModules;

export type HardwareKeyEnrollmentMaterial = {
  alias: string;
  publicKeyBase64: string;
  certificateChainBase64: string[];
  hardwareSecurityLevel?: string;
};

export type HardwareSignatureResult = {
  alias: string;
  signatureBase64: string;
};

function ensureAndroid(): void {
  if (Platform.OS !== 'android') {
    throw new Error('Hardware attestation is only supported on Android.');
  }
}

function ensureNativeModule(): void {
  if (!HardwareAttestation) {
    throw new Error('HardwareAttestation native module is not available.');
  }
}

export async function generateKeyWithAttestation(
  alias: string,
  challengeBase64: string,
): Promise<HardwareKeyEnrollmentMaterial> {
  ensureAndroid();
  ensureNativeModule();

  return HardwareAttestation.generateKeyWithAttestation(alias, challengeBase64);
}

export async function signChallenge(
  alias: string,
  challengeBase64: string,
): Promise<HardwareSignatureResult> {
  ensureAndroid();
  ensureNativeModule();

  return HardwareAttestation.signChallenge(alias, challengeBase64);
}

export async function deleteHardwareKey(alias: string): Promise<boolean> {
  ensureAndroid();
  ensureNativeModule();

  return HardwareAttestation.deleteKey(alias);
}

export type DeviceIdentity = {
  deviceId: string;
  deviceName: string;
  manufacturer: string;
  brand: string;
  model: string;
  device: string;
  product: string;
  hardware: string;
  androidVersion: string;
  sdkInt: number;
  supportedAbis: string[];
  isEmulator: boolean;
  cpuCoreCount: number;
  totalMemoryBytes: number;
};

export async function getDeviceIdentity(): Promise<DeviceIdentity> {
  ensureAndroid();
  ensureNativeModule();

  return HardwareAttestation.getDeviceIdentity();
}
