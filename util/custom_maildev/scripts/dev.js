const nodemon = require('nodemon')
const sendEmails = require('./send.js')

nodemon({
  script: './bin/maildev',
  verbose: true,
  watch: [
    'index.js',
    'lib/*'
  ],
  args: [
    '--verbose',
    '--outgoing-host', '192.168.1.196',
    '--outgoing-port', '2025',
    '--auto-relay'

  ]
}).on('start', function () {
  setTimeout(sendEmails, 1000)
}).on('crash', function () {
  console.log('Nodemon process crashed')
})
