import React, { useState } from 'react';
import {
  Button,
  SafeAreaView,
  StyleSheet,
  View,
} from 'react-native';

import { SoftwareAttestationScreen } from './src/screens/SoftwareAttestationScreen';
import { HardwareAttestationScreen } from './src/screens/HardwareAttestationScreen';

type ActiveScreen = 'software' | 'hardware';

function App(): React.JSX.Element {
  const [activeScreen, setActiveScreen] = useState<ActiveScreen>('software');

  return (
    <SafeAreaView style={styles.root}>
      <View style={styles.tabContainer}>
        <View style={styles.tabButton}>
          <Button
            title="Software"
            onPress={() => setActiveScreen('software')}
            disabled={activeScreen === 'software'}
          />
        </View>

        <View style={styles.tabButton}>
          <Button
            title="Hardware"
            onPress={() => setActiveScreen('hardware')}
            disabled={activeScreen === 'hardware'}
          />
        </View>
      </View>

      {activeScreen === 'software' ? (
        <SoftwareAttestationScreen />
      ) : (
        <HardwareAttestationScreen />
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
  },
  tabContainer: {
    flexDirection: 'row',
    gap: 12,
    padding: 16,
  },
  tabButton: {
    flex: 1,
  },
});

export default App;