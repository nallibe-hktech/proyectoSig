// Karma configuration file
// https://karma-runner.github.io/6.4/config/configuration-file.html

module.exports = function (config) {
  // En Windows muchas máquinas no tienen Chrome; usa Edge (Chromium-based) si está instalado.
  // Si tienes Chrome, simplemente exporta CHROME_BIN apuntando a chrome.exe.
  const candidatePaths = [
    process.env.CHROME_BIN,
    'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
    'C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe',
  ].filter(Boolean);
  for (const p of candidatePaths) {
    try {
      const fs = require('fs');
      if (fs.existsSync(p)) {
        process.env.CHROME_BIN = p;
        break;
      }
    } catch { /* ignore */ }
  }

  config.set({
    basePath: '',
    frameworks: ['jasmine', '@angular/build'],
    plugins: [
      require('karma-jasmine'),
      require('karma-chrome-launcher'),
      require('karma-jasmine-html-reporter'),
      require('karma-coverage'),
    ],
    client: {
      jasmine: {
        random: false,
        timeoutInterval: 10000,
      },
      clearContext: false,
    },
    jasmineHtmlReporter: { suppressAll: true },
    coverageReporter: {
      dir: require('path').join(__dirname, './coverage'),
      subdir: '.',
      reporters: [{ type: 'html' }, { type: 'text-summary' }, { type: 'lcov' }],
    },
    reporters: ['progress', 'kjhtml'],
    port: 9876,
    colors: true,
    logLevel: config.LOG_INFO,
    autoWatch: false,
    singleRun: true,
    restartOnFileChange: false,
    browsers: ['ChromeHeadlessCI'],
    customLaunchers: {
      ChromeHeadlessCI: {
        base: 'ChromeHeadless',
        flags: ['--no-sandbox', '--disable-gpu', '--disable-dev-shm-usage'],
      },
    },
    browserDisconnectTimeout: 30000,
    browserNoActivityTimeout: 60000,
  });
};
