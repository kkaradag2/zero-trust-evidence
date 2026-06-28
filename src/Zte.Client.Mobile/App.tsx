import React from 'react';
import {
  SafeAreaView,
  StyleSheet,
} from 'react-native';

import { BenchmarkScreen } from './src/screens/BenchmarkScreen';

function App(): React.JSX.Element {
  return (
    <SafeAreaView style={styles.root}>
      <BenchmarkScreen />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
  },
});

export default App;
