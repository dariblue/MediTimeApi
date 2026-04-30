const crypto = require('crypto');
const ecdh = crypto.createECDH('prime256v1');
ecdh.generateKeys();

const publicKeyBase64 = ecdh.getPublicKey().toString('base64');
const privateKeyBase64 = ecdh.getPrivateKey().toString('base64');

// Convert to URL-safe base64
const toUrlSafe = (b64) => b64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');

console.log('=== VAPID Keys for MediTime ===');
console.log('');
console.log('PUBLIC_KEY (base64url):');
console.log(toUrlSafe(publicKeyBase64));
console.log('');
console.log('PRIVATE_KEY (base64url):');
console.log(toUrlSafe(privateKeyBase64));
console.log('');
console.log('PUBLIC_KEY (standard base64 for C#):');
console.log(publicKeyBase64);
console.log('');
console.log('PRIVATE_KEY (standard base64 for C#):');
console.log(privateKeyBase64);
