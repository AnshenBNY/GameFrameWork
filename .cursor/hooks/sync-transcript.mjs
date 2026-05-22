/**
 * afterAgentResponse: 读取 Cursor 转写 jsonl → 导出为近似「Export Transcript」的 Markdown
 * 输出 chat/YYYYMMDD--<conversation_id>.md（日期取自转写文件/目录的创建时间，无旁路配置文件）
 */
import fs from "fs/promises";
import path from "path";
import { fileURLToPath } from "url";

async function writeHookRunLog(payload, workspaceRoot) {
  const root =
    workspaceRoot ||
    normalizeCursorWindowsPath(process.env.CURSOR_PROJECT_DIR || "") ||
    process.cwd();
  const logPath = path.join(root, ".cursor", "hooks", "last-run.json");
  try {
    await fs.mkdir(path.dirname(logPath), { recursive: true });
    await fs.writeFile(
      logPath,
      JSON.stringify({ ...payload, at: new Date().toISOString() }, null, 2),
      "utf8",
    );
  } catch {
    /* ignore */
  }
}

/** 将单条转写导出到 workspaceRoot/chat/（Hook 与终端监听共用） */
export async function exportSession({
  conversationId,
  transcriptPath,
  workspaceRoot,
  cursorVersion = "",
}) {
  const chatDir = path.join(workspaceRoot, "chat");
  const safeId = sanitizeSegment(conversationId);
  let ymdPrefix;
  try {
    ymdPrefix = await getYyyymmddPrefixForSession(transcriptPath);
  } catch (e) {
    return {
      ok: false,
      phase: "date_prefix_failed",
      error: String(e?.message ?? e),
    };
  }
  const destPath = path.join(chatDir, `${ymdPrefix}--${safeId}.md`);

  let buf = null;
  try {
    buf = await readFileStable(transcriptPath, {
      retries: 12,
      delayMs: 80,
    });
  } catch (e) {
    return {
      ok: false,
      phase: "read_transcript_failed",
      error: String(e?.message ?? e),
    };
  }

  try {
    await fs.mkdir(chatDir, { recursive: true });
  } catch (e) {
    return { ok: false, phase: "mkdir_failed", error: String(e?.message ?? e) };
  }

  try {
    const md = transcriptJsonlToExportMarkdown(buf.toString("utf8"), {
      cursorVersion: cursorVersion || "",
    });
    await writeFileStable(destPath, Buffer.from(md, "utf8"), {
      retries: 8,
      delayMs: 60,
    });
    return {
      ok: true,
      phase: "ok",
      dest_path: destPath,
      conversation_id: conversationId,
      transcript_path: transcriptPath,
      workspace_root: workspaceRoot,
    };
  } catch (e) {
    return {
      ok: false,
      phase: "write_failed",
      dest_path: destPath,
      error: String(e?.message ?? e),
    };
  }
}

async function main() {
  let input;
  const { text: raw } = await readStdinBufferUtf8();
  await writeHookRunLog({ phase: "started", stdin_bytes: raw?.length ?? 0 });

  const parsed = parseAfterAgentResponseStdin(raw);
  if (!parsed.ok) {
    await writeHookRunLog({
      phase: "parse_failed",
      error: parsed.error,
      attempts: parsed.fallback_attempts,
    });
    process.exit(0);
  }
  input = parsed.value;

  const conversationId =
    (input.conversation_id && String(input.conversation_id).trim()) ||
    (input.session_id && String(input.session_id).trim()) ||
    null;
  const transcriptPath = await resolveTranscriptPath(conversationId, {
    transcriptPath:
      (input.transcript_path && String(input.transcript_path).trim()) ||
      (process.env.CURSOR_TRANSCRIPT_PATH &&
        process.env.CURSOR_TRANSCRIPT_PATH.trim()) ||
      null,
  });

  if (!conversationId || !transcriptPath) {
    await writeHookRunLog(
      {
        phase: "missing_fields",
        conversation_id: conversationId,
        transcript_path: transcriptPath,
        recovery: parsed.recovery ?? null,
      },
      resolveWorkspaceRoot(input),
    );
    process.exit(0);
  }

  const workspaceRoot = resolveWorkspaceRoot(input);
  const result = await exportSession({
    conversationId,
    transcriptPath,
    workspaceRoot,
    cursorVersion:
      (input.cursor_version && String(input.cursor_version)) ||
      process.env.CURSOR_VERSION ||
      "",
  });
  await writeHookRunLog(result, workspaceRoot);
  process.exit(0);
}

function sanitizeSegment(id) {
  return String(id).replace(/[/\\:?*"<>|]/g, "_");
}

/** 文件名日期前缀：用转写文件的「创建时间」近似会话首开，不读任何旁路配置。追加写入 jsonl 时 birthtime 通常不变。 */
async function getYyyymmddPrefixForSession(transcriptPath) {
  const st = await fs.stat(transcriptPath);
  if (isReasonableSessionAnchor(st.birthtime)) {
    return formatYyyymmddAsiaShanghai(st.birthtime);
  }
  /** 部分 Linux 上文件 birthtime 不可用：尝试会话目录（agent-transcripts/<id>/）的创建时间 */
  try {
    const dst = await fs.stat(path.dirname(transcriptPath));
    if (isReasonableSessionAnchor(dst.birthtime)) {
      return formatYyyymmddAsiaShanghai(dst.birthtime);
    }
    if (isReasonableSessionAnchor(dst.ctime)) {
      return formatYyyymmddAsiaShanghai(dst.ctime);
    }
  } catch (_) {
    /* ignore */
  }
  return formatYyyymmddAsiaShanghai(
    isReasonableSessionAnchor(st.ctime) ? st.ctime : st.mtime,
  );
}

function isReasonableSessionAnchor(d) {
  if (!d || !(d instanceof Date)) return false;
  const y = d.getFullYear();
  const t = d.getTime();
  return y >= 2018 && y <= 2040 && Number.isFinite(t);
}

function formatYyyymmddAsiaShanghai(date) {
  const s = new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Shanghai",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(date);
  return s.replace(/-/g, "");
}

function normalizeCursorWindowsPath(p) {
  const s = String(p).trim();
  if (!s) return s;
  if (process.platform === "win32") {
    /** Cursor 多根/工作区常传 /d:/path/to/project */
    const m = s.match(/^\/+([A-Za-z]:)(?:\/(.*))?$/i);
    if (m) {
      const rest = m[2] ? m[2].replace(/\//g, "\\") : "";
      return rest ? `${m[1]}\\${rest}` : `${m[1]}\\`;
    }
  }
  return s;
}

/** stdin / 环境变量未提供 transcript_path 时，在 ~/.cursor/projects/*/agent-transcripts 下按会话 ID 查找 */
async function resolveTranscriptPath(conversationId, { transcriptPath }) {
  if (transcriptPath) {
    return normalizeCursorWindowsPath(transcriptPath);
  }
  const id = conversationId && sanitizeSegment(conversationId);
  if (!id) return null;

  const home =
    (process.env.USERPROFILE && process.env.USERPROFILE.trim()) ||
    (process.env.HOME && process.env.HOME.trim()) ||
    null;
  if (!home) return null;

  const projectsRoot = path.join(home, ".cursor", "projects");
  let entries = [];
  try {
    entries = await fs.readdir(projectsRoot, { withFileTypes: true });
  } catch {
    return null;
  }

  const rel = path.join("agent-transcripts", id, `${id}.jsonl`);
  for (const ent of entries) {
    if (!ent.isDirectory()) continue;
    const candidate = path.join(projectsRoot, ent.name, rel);
    try {
      await fs.access(candidate);
      return candidate;
    } catch {
      /* try next project slug */
    }
  }
  return null;
}

async function delay(ms) {
  return new Promise((r) => setTimeout(r, ms));
}

async function copyFileWithRetries(src, dest, { retries, delayMs }) {
  let last;
  for (let i = 0; i < retries; i++) {
    try {
      await fs.copyFile(src, dest);
      return;
    } catch (e) {
      last = e;
      await delay(delayMs);
    }
  }
  throw last;
}

async function readFileStable(srcPath, { retries, delayMs }) {
  let last;
  for (let i = 0; i < retries; i++) {
    try {
      return await fs.readFile(srcPath);
    } catch (e) {
      last = e;
      await delay(delayMs);
    }
  }
  /** Windows 上源文件可能被独占：复制到同目录临时文件再读 */
  const tmp =
    path.join(path.dirname(srcPath), `.hook-read-${process.pid}-${Date.now()}`) +
    path.extname(srcPath);
  try {
    await copyFileWithRetries(srcPath, tmp, { retries: 8, delayMs });
    try {
      return await fs.readFile(tmp);
    } finally {
      await fs.unlink(tmp).catch(() => {});
    }
  } catch {
    throw last;
  }
}

async function writeFileStable(destPath, data, { retries, delayMs }) {
  let last;
  for (let i = 0; i < retries; i++) {
    try {
      await fs.writeFile(destPath, data);
      return;
    } catch (e) {
      last = e;
      await delay(delayMs);
    }
  }
  throw last;
}

function resolveWorkspaceRoot(input) {
  const fromHook =
    Array.isArray(input.workspace_roots) &&
    input.workspace_roots[0] &&
    String(input.workspace_roots[0]).trim();
  const fromEnv =
    process.env.CURSOR_PROJECT_DIR && process.env.CURSOR_PROJECT_DIR.trim();
  const raw = fromHook || fromEnv || "";
  const normalized = normalizeCursorWindowsPath(raw);
  return normalized || process.cwd();
}

async function readStdinBufferUtf8() {
  const buf = await new Promise((resolve, reject) => {
    const chunks = [];
    process.stdin.on("data", (d) => {
      chunks.push(Buffer.isBuffer(d) ? d : Buffer.from(d));
    });
    process.stdin.on("end", () => resolve(Buffer.concat(chunks)));
    process.stdin.on("error", reject);
  });
  let trimmed = buf;
  let utf8BomStrips = 0;
  while (
    trimmed.length >= 3 &&
    trimmed[0] === 0xef &&
    trimmed[1] === 0xbb &&
    trimmed[2] === 0xbf
  ) {
    trimmed = trimmed.subarray(3);
    utf8BomStrips++;
  }
  let text = trimmed.toString("utf8");
  while (text.length && text.charCodeAt(0) === 0xfeff) {
    text = text.slice(1);
  }
  return {
    text,
    notes: { stdin_bytes: buf.length, stripped_utf8_bom: utf8BomStrips },
  };
}

/** Hook stdin 中 `text` 可能含破坏 JSON 的字节；先严谨 parse，再清空 text，再宽松抽字段 */
function parseAfterAgentResponseStdin(raw) {
  const attempts = [];
  let strictErr = "";

  if (!raw || !String(raw).trim()) {
    return {
      ok: false,
      error: "empty stdin",
      fallback_attempts: ["empty"],
    };
  }

  try {
    attempts.push("strict_json");
    const value = JSON.parse(raw);
    return { ok: true, value, recovery: null, fallback_attempts: attempts };
  } catch (e) {
    strictErr = String(e?.message ?? e);
    attempts.push(`strict_json_failed:${truncateErr(strictErr)}`);
  }

  try {
    attempts.push("blank_text_field");
    const cleared = blankFirstJsonStringField(raw, "text");
    const value = JSON.parse(cleared);
    return {
      ok: true,
      value,
      recovery: "blank_text_field",
      fallback_attempts: attempts,
    };
  } catch (e2) {
    attempts.push(`blank_text_failed:${truncateErr(String(e2?.message ?? e2))}`);
  }

  try {
    attempts.push("regex_loose_extract");
    const conversation_id =
      extractStringPropertyLoose(raw, "conversation_id") ||
      extractStringPropertyLoose(raw, "session_id") ||
      extractUuidLoose(raw);
    const transcript_path = extractStringPropertyLoose(raw, "transcript_path");
    const cursor_version =
      extractStringPropertyLoose(raw, "cursor_version") ||
      process.env.CURSOR_VERSION ||
      "";
    const ws = extractWorkspaceRootsLoose(raw);
    if (conversation_id) {
      return {
        ok: true,
        value: {
          conversation_id,
          ...(transcript_path ? { transcript_path } : {}),
          cursor_version,
          ...(ws?.length ? { workspace_roots: ws } : {}),
        },
        recovery: "regex_extract",
        fallback_attempts: attempts,
      };
    }
    attempts.push("regex_loose_missing_id");
  } catch (e3) {
    attempts.push(`regex_loose_throw:${truncateErr(String(e3?.message ?? e3))}`);
  }

  return {
    ok: false,
    error: truncateErr(strictErr || "unknown") + ` :: ${attempts.join(" · ")}`,
    fallback_attempts: attempts,
  };
}

function truncateErr(msg, max = 220) {
  const s = String(msg);
  return s.length <= max ? s : s.slice(0, max) + "…";
}

function blankFirstJsonStringField(raw, field) {
  const needle = `"${field}"`;
  let from = 0;
  while (from < raw.length) {
    const kp = raw.indexOf(needle, from);
    if (kp === -1) return raw;
    let p = kp + needle.length;
    while (p < raw.length && /\s/.test(raw[p])) p++;
    if (raw[p] !== ":") {
      from = kp + needle.length;
      continue;
    }
    p++;
    while (p < raw.length && /\s/.test(raw[p])) p++;
    if (raw[p] !== '"') {
      from = kp + needle.length;
      continue;
    }
    const opened = p;
    const sk = skipJsonQuotedStringPreserve(raw, opened);
    if (!sk) return raw;
    return raw.slice(0, opened + 1) + raw.slice(sk.closeIdx);
  }
  return raw;
}

function skipJsonQuotedStringPreserve(raw, openQuoteIdx) {
  if (raw[openQuoteIdx] !== '"') return null;
  let p = openQuoteIdx + 1;
  while (p < raw.length) {
    const ch = raw[p];
    if (ch === "\\") {
      p += 2;
      continue;
    }
    if (ch === '"') return { closeIdx: p };
    p++;
  }
  return null;
}

/** 在非严格 JSON 中按 key 读出字符串（处理转义） */
function extractStringPropertyLoose(raw, key) {
  const needle = `"${key}"`;
  let from = 0;
  while (from < raw.length) {
    const kp = raw.indexOf(needle, from);
    if (kp === -1) return null;
    let p = kp + needle.length;
    while (p < raw.length && /\s/.test(raw[p])) p++;
    if (raw[p] !== ":") {
      from = kp + needle.length;
      continue;
    }
    p++;
    while (p < raw.length && /\s/.test(raw[p])) p++;
    if (raw[p] !== '"') {
      from = kp + needle.length;
      continue;
    }
    const rd = readJsonQuotedStringDecoded(raw, p);
    if (!rd.ok) return null;
    return rd.value;
  }
  return null;
}

function readJsonQuotedStringDecoded(raw, openQuoteIdx) {
  if (raw[openQuoteIdx] !== '"') return { ok: false, value: "" };
  let out = "";
  let p = openQuoteIdx + 1;
  while (p < raw.length) {
    const ch = raw[p];
    if (ch === "\\") {
      if (p + 1 >= raw.length) break;
      const nx = raw[p + 1];
      if (
        nx === "u" &&
        /^[0-9a-fA-F]{4}$/.test(raw.slice(p + 2, p + 6))
      ) {
        out += String.fromCharCode(Number.parseInt(raw.slice(p + 2, p + 6), 16));
        p += 6;
        continue;
      }
      switch (nx) {
        case '"':
          out += '"';
          break;
        case "\\":
          out += "\\";
          break;
        case "/":
          out += "/";
          break;
        case "b":
          out += "\b";
          break;
        case "f":
          out += "\f";
          break;
        case "n":
          out += "\n";
          break;
        case "r":
          out += "\r";
          break;
        case "t":
          out += "\t";
          break;
        default:
          out += nx;
      }
      p += 2;
      continue;
    }
    if (ch === '"') return { ok: true, value: out };
    out += ch;
    p++;
  }
  return { ok: false, value: out };
}

function extractUuidLoose(raw) {
  const m = raw.match(
    /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i,
  );
  return m ? m[0] : null;
}

function extractWorkspaceRootsLoose(raw) {
  const needle = '"workspace_roots"';
  const k = raw.indexOf(needle);
  if (k === -1) return null;
  let i = k + needle.length;
  while (i < raw.length && /\s/.test(raw[i])) i++;
  if (raw[i] !== ":") return null;
  i++;
  while (i < raw.length && /\s/.test(raw[i])) i++;
  if (raw[i] !== "[") return null;
  let depth = 0;
  const start = i;
  for (; i < raw.length; i++) {
    const c = raw[i];
    if (c === "[") depth++;
    else if (c === "]") {
      depth--;
      if (depth === 0) {
        try {
          return JSON.parse(raw.slice(start, i + 1));
        } catch {
          return null;
        }
      }
    }
  }
  return null;
}

function buildDefaultTitle(workspaceRoot) {
  try {
    const b = path.basename(path.resolve(workspaceRoot));
    if (b && b !== "." && b !== "..") return `Chat transcript — ${b}`;
  } catch (_) {
    /* ignore */
  }
  return "Chat transcript";
}

/** 对齐手写 Export：一级标题 → 导出时间 → --- → (**User**|**Cursor**) + 正文；连续同色行合并 */

function transcriptJsonlToExportMarkdown(
  jsonlUtf8,
  { cursorVersion, conversationTitle },
) {
  const rows = [];
  for (const line of jsonlUtf8.split(/\r?\n/)) {
    const t = line.trim();
    if (!t) continue;
    try {
      rows.push(JSON.parse(t));
    } catch {
      /* 跳过损坏行 */
    }
  }

  const merged = [];
  for (const row of rows) {
    const role = row.role;
    if (role !== "user" && role !== "assistant") continue;
    const body = finalizeRoleBody(role, extractTextParts(row.message ?? {}));
    if (body.trim() === "") continue;
    const prev = merged[merged.length - 1];
    if (prev && prev.role === role) {
      prev.body = `${prev.body}\n\n${body}`;
    } else {
      merged.push({ role, body });
    }
  }

  const h1 = `# ${(conversationTitle && String(conversationTitle).trim()) || "Chat transcript"}`;
  const exported = formatExportedLine(cursorVersion ?? "");
  const blocks = merged.map((m) => {
    const who = m.role === "user" ? "**User**" : "**Cursor**";
    return `---\n\n${who}\n\n${m.body.trim()}\n\n`;
  });
  /** 与用户样例：标题与斜体之间空一行，再 --- */
  const body =
    `${h1}\n\n${exported}\n\n---\n\n${blocks.join("")}`.trimEnd() + `\n`;

  return body;
}

function extractTextParts(message) {
  const content = message && message.content;
  if (!Array.isArray(content)) return "";
  const parts = [];
  for (const block of content) {
    if (block && block.type === "text" && typeof block.text === "string") {
      parts.push(block.text);
    }
  }
  return parts.join("");
}

function finalizeRoleBody(role, rawText) {
  let t = String(rawText);
  if (role === "user") t = stripUserQueryEnvelope(t);
  t = scrubRedactedPlaceholders(t);
  /** 合并多余空白，保留段落 */
  return t.replace(/\r\n/g, "\n").replace(/[ \t]+\n/g, "\n");
}

function stripUserQueryEnvelope(s) {
  const m = String(s).match(
    /^[\s\n]*<user_query>([\s\S]*?)<\/user_query>[\s\n]*$/,
  );
  return m ? m[1].trim() : String(s).trim();
}

/** 手写 Export 不出现 [REDACTED]，按占位整块去掉 */
function scrubRedactedPlaceholders(s) {
  return (
    String(s)
      /** 独立一行或夹在换行间的 REDACTED */
      .replace(/\n*\s*\[REDACTED\]\s*\n*/g, "\n\n")
      .replace(/\n{3,}/g, "\n\n")
      .trim()
  );
}

/** 与用户样例：`2026/5/15 at GMT+8 16:18:54` */
function formatExportedLine(cursorVersion) {
  const d = utcPlusOffset(new Date(), 8);
  const pad = (n) => String(n).padStart(2, "0");
  const y = d.year;
  const mo = d.month + 1;
  const da = d.day;
  const h = pad(d.hour);
  const mi = pad(d.minute);
  const se = pad(d.second);
  const verEsc = cursorVersion || "?";
  return `_Exported on ${y}/${mo}/${da} at GMT+8 ${h}:${mi}:${se} from Cursor (${verEsc})_`;
}

function utcPlusOffset(date, offsetHours) {
  const utcMs = date.getTime() + date.getTimezoneOffset() * 60_000;
  const shifted = new Date(utcMs + offsetHours * 3600_000);
  return {
    year: shifted.getUTCFullYear(),
    month: shifted.getUTCMonth(),
    day: shifted.getUTCDate(),
    hour: shifted.getUTCHours(),
    minute: shifted.getUTCMinutes(),
    second: shifted.getUTCSeconds(),
  };
}

const isDirectRun =
  process.argv[1] &&
  path.resolve(fileURLToPath(import.meta.url)) === path.resolve(process.argv[1]);

if (isDirectRun) {
  main().catch(() => process.exit(0));
}
