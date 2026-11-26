// Prints a JWT for local testing in Swagger. No dependencies; needs Node.js.
//
//   node scripts/make-dev-token.js admin     -> Security Administrator (can read AND record telemetry)
//   node scripts/make-dev-token.js officer   -> Police Officer (can read only)
//   node scripts/make-dev-token.js <user-id> -> any user_account.user_id
//
// Reads JWT_SECRET (and optionally JWT_AUDIENCE) from .env in the project root, or from the environment.
// Run it from the project root. The token is valid for 8 hours.
const crypto = require("crypto");
const fs = require("fs");
const path = require("path");

const DEV_USERS = {
  admin: "d0000000-0000-4000-8000-000000000001",
  officer: "d0000000-0000-4000-8000-000000000002",
};

function readEnvFile() {
  const file = path.join(process.cwd(), ".env");
  const values = {};
  if (!fs.existsSync(file)) return values;
  for (const line of fs.readFileSync(file, "utf8").split(/\r?\n/)) {
    const m = line.match(/^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$/);
    if (m) values[m[1]] = m[2].replace(/^["']|["']$/g, "");
  }
  return values;
}

const env = { ...readEnvFile(), ...process.env };
const secret = env.JWT_SECRET;
if (!secret) {
  console.error("JWT_SECRET not found. Run from the project root where .env is, or set JWT_SECRET.");
  process.exit(1);
}

const who = process.argv[2] || "admin";
const sub = DEV_USERS[who] || who;
const b64 = (obj) => Buffer.from(JSON.stringify(obj)).toString("base64url");
const now = Math.floor(Date.now() / 1000);

const header = { alg: "HS256", typ: "JWT" };
const payload = {
  sub,
  iss: "CmRegistrationService",
  aud: env.JWT_AUDIENCE || "CmRegistrationClients",
  iat: now,
  exp: now + 8 * 60 * 60,
};

const unsigned = `${b64(header)}.${b64(payload)}`;
const signature = crypto.createHmac("sha256", secret).update(unsigned).digest("base64url");
console.log(`${unsigned}.${signature}`);
