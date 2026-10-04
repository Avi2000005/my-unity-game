#!/usr/bin/env node
/**
 * Minimal MCP client for Unity's official `unity mcp` stdio server.
 *
 * opencode has this MCP server registered but dropped it after a domain reload
 * and will not reconnect inside the current session. The server itself is fine,
 * so this bridges the gap: it performs the handshake, calls a tool, prints the
 * result and exits.
 *
 *   node unity-mcp.js list
 *   node unity-mcp.js call <tool-name> '<json arguments>'
 *
 * Unity sits at https://127.0.0.1:7800 behind this server; every call inherits
 * the same 60s ceiling as before, so callers should stay short.
 */
'use strict';

const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');

const UNITY = 'C:\\Users\\chate\\AppData\\Local\\Unity\\bin\\unity.exe';
const PROJECT = 'C:\\Users\\chate\\Desktop\\Echos_of_Forgotten_Painter';
// Screenshots land here. Prefer the pre-approved temp directory over the
// project, and create it — it may not exist on a fresh machine.
const TEMP =
  process.env.MCP_TEMP_DIR ||
  path.join(
    process.env.LOCALAPPDATA || process.env.TMP || '.',
    'Temp',
    'opencode'
  );
const TIMEOUT_MS = Number(process.env.MCP_TIMEOUT_MS || 55000);

try {
  fs.mkdirSync(TEMP, { recursive: true });
} catch (e) {
  /* a read-only temp dir is not fatal until an image is actually needed */
}

function main() {
  const mode = process.argv[2];           // 'list' | 'call'
  const tool = process.argv[3];
  let args = {};
  if (mode === 'call') {
    // Prefer the environment. PowerShell mangles quoted JSON passed as a native
    // command argument (the double quotes are consumed by the shell), which
    // arrives here as {file:...} and fails to parse with a confusing error at
    // column 2. MCP_ARGS sidesteps the shell entirely.
    const raw = process.env.MCP_ARGS || process.argv[4] || '{}';
    try {
      args = JSON.parse(raw);
    } catch (e) {
      console.error('arguments must be valid JSON: ' + e.message);
      console.error('received: ' + raw.slice(0, 200));
      process.exit(2);
    }
  }

  const proc = spawn(UNITY, ['mcp', '--project-path', PROJECT], {
    stdio: ['pipe', 'pipe', 'pipe'],
    windowsHide: true,
  });

  let buf = '';
  let done = false;
  const pending = [];

  const kill = (code) => {
    if (done) return;
    done = true;
    clearTimeout(timer);
    try { proc.kill(); } catch (e) { /* already gone */ }
    process.exit(code);
  };

  const timer = setTimeout(() => {
    console.error('timeout after ' + TIMEOUT_MS + 'ms');
    kill(1);
  }, TIMEOUT_MS);

  const send = (obj) => proc.stdin.write(JSON.stringify(obj) + '\n');

  proc.stdout.on('data', (d) => {
    buf += d.toString();
    let nl;
    // Newline-delimited JSON-RPC. Coalesce anything the server split across
    // writes rather than assuming one chunk is one message.
    while ((nl = buf.indexOf('\n')) >= 0) {
      const line = buf.slice(0, nl).trim();
      buf = buf.slice(nl + 1);
      if (!line) continue;
      let msg;
      try { msg = JSON.parse(line); } catch (e) { continue; }
      if (msg.id === 1 && msg.result) {
        send({ jsonrpc: '2.0', method: 'notifications/initialized' });
        if (mode === 'list') {
          send({ jsonrpc: '2.0', id: 2, method: 'tools/list', params: {} });
        } else {
          send({
            jsonrpc: '2.0',
            id: 2,
            method: 'tools/call',
            params: { name: tool, arguments: args },
          });
        }
      } else if (msg.id === 2) {
        // tools/list returns {result:{tools:[...]}},
        // tools/call returns {result:{content:[...]}} or {error:{...}}.
        // Screenshots come back as {type:'image', data:<base64>}, which has no
        // .text — mapping only c.text would print an empty string and lose the
        // image silently. Decode those to disk instead.
        const parts = [];
        if (msg.result && msg.result.tools) {
          parts.push(
            ...msg.result.tools.map(
              (t) => t.name + ' :: ' + (t.description || '').slice(0, 90)
            )
          );
        } else if (msg.result && msg.result.content) {
          let imgIndex = 0;
          for (const c of msg.result.content) {
            if (c.type === 'image' && c.data) {
              const p = path.join(TEMP, 'shot_' + Date.now() + '_' + imgIndex++ + '.png');
              fs.writeFileSync(p, Buffer.from(c.data, 'base64'));
              parts.push('[image saved] ' + p);
            } else if (c.text) {
              parts.push(c.text);
            } else {
              parts.push(JSON.stringify(c));
            }
          }
        } else {
          parts.push(JSON.stringify(msg.error || msg.result, null, 2));
        }
        // An empty content array would otherwise print a blank line and be
        // indistinguishable from a no-op. Show the raw envelope instead.
        if (parts.length === 0) parts.push(JSON.stringify(msg.result, null, 2));
        console.log(parts.join('\n'));
        kill(0);
      }
    }
  });

  proc.stderr.on('data', (d) => {
    // The banner is noise; anything that looks like an error is worth keeping.
    const s = d.toString();
    if (/error|exception|failed/i.test(s)) process.stderr.write(s);
  });

  proc.on('error', (e) => {
    console.error('spawn failed: ' + e.message);
    kill(1);
  });

  proc.on('exit', (code) => {
    if (done) return;
    console.error('server exited with ' + code + ' before responding');
    kill(1);
  });

  send({
    jsonrpc: '2.0',
    id: 1,
    method: 'initialize',
    params: {
      protocolVersion: '2024-11-05',
      capabilities: {},
      clientInfo: { name: 'direct-bridge', version: '1.0.0' },
    },
  });
}

main();
