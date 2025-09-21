// Kills any process bound to TCP port 5252 on Windows. Safe no-op if none found.
const { execSync } = require('child_process');

function run(cmd) {
  try {
    return execSync(cmd, { stdio: ['ignore', 'pipe', 'pipe'] }).toString();
  } catch (e) {
    return '';
  }
}

try {
  // Find PIDs listening on 5252 (IPv4/IPv6)
  const out = run('netstat -ano | findstr :5252');
  if (!out) {
    console.log('[kill-port] No process found on :5252');
    process.exit(0);
  }

  // Parse lines like:
  //  TCP    127.0.0.1:5252   0.0.0.0:0   LISTENING   36356
  //  TCP    [::1]:5252       [::]:0      LISTENING   36356
  const pids = Array.from(new Set(out
    .split(/\r?\n/)
    .map(l => l.trim())
    .filter(Boolean)
    .map(l => l.split(/\s+/))
    .map(cols => cols[cols.length - 1])
    .filter(pid => /^(\d+)$/.test(pid))
  ));

  if (pids.length === 0) {
    console.log('[kill-port] No PID parsed from netstat output');
    process.exit(0);
  }

  for (const pid of pids) {
    try {
      console.log(`[kill-port] Killing PID ${pid} on :5252...`);
      run(`taskkill /PID ${pid} /F`);
    } catch (e) {
      // ignore failures
    }
  }

  console.log('[kill-port] Done');
} catch (err) {
  console.log('[kill-port] Error:', err?.message || err);
  // Never fail the build if this cleanup fails
  process.exit(0);
}
