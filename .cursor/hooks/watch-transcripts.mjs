/**
 * 轮询 Cursor 转写目录并同步到 chat/（绕过 Windows Hook spawn EPERM）
 * 用法（在项目根目录）:
 *   node .cursor/hooks/init-watch-config.mjs   # 首次
 *   node .cursor/hooks/watch-transcripts.mjs   # 保持运行
 */
import fs from "fs/promises";
import path from "path";
import { exportSession } from "./sync-transcript.mjs";

const POLL_MS = 2000;
const configPath = path.join(
  process.cwd(),
  ".cursor",
  "hooks",
  "watch-config.json",
);

async function loadConfig() {
  const raw = await fs.readFile(configPath, "utf8");
  return JSON.parse(raw);
}

async function listMainTranscripts(transcriptsDir) {
  const out = [];
  let entries = [];
  try {
    entries = await fs.readdir(transcriptsDir, { withFileTypes: true });
  } catch {
    return out;
  }
  for (const ent of entries) {
    if (!ent.isDirectory()) continue;
    const id = ent.name;
    const jsonl = path.join(transcriptsDir, id, `${id}.jsonl`);
    try {
      const st = await fs.stat(jsonl);
      out.push({ id, path: jsonl, mtimeMs: st.mtimeMs });
    } catch {
      /* 跳过 subagents 等 */
    }
  }
  return out;
}

const config = await loadConfig();
const { workspaceRoot, transcriptsDir } = config;
const lastMtime = new Map();

console.log("监听转写目录:", transcriptsDir);
console.log("导出到:", path.join(workspaceRoot, "chat"));
console.log(`每 ${POLL_MS}ms 检查一次，Ctrl+C 结束\n`);

async function tick() {
  const files = await listMainTranscripts(transcriptsDir);
  for (const f of files) {
    const prev = lastMtime.get(f.path) ?? 0;
    if (f.mtimeMs <= prev) continue;
    lastMtime.set(f.path, f.mtimeMs);
    const result = await exportSession({
      conversationId: f.id,
      transcriptPath: f.path,
      workspaceRoot,
      cursorVersion: process.env.CURSOR_VERSION || "watch",
    });
    if (result.ok) {
      console.log(`[ok] ${path.basename(result.dest_path)}`);
    } else {
      console.log(`[${result.phase}] ${f.id}: ${result.error ?? ""}`);
    }
  }
}

await tick();
setInterval(() => {
  tick().catch((e) => console.error(e.message));
}, POLL_MS);
