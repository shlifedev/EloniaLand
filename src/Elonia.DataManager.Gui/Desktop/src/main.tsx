import { invoke } from '@tauri-apps/api/core';
import { useEffect, useRef, useState } from 'react';
import { createRoot } from 'react-dom/client';
import {
  Archive,
  Braces,
  Check,
  CheckCheck,
  CircleAlert,
  CircleMinus,
  Database,
  ExternalLink,
  FileSpreadsheet,
  FolderOpen,
  Layers2,
  List,
  Package,
  RefreshCw,
  Search,
  Sheet as SheetIcon,
  Star,
  Table2,
  TriangleAlert,
  X,
} from 'lucide-react';
import Activity from './Activity';
import {
  kinds,
  lastResults,
  makeLog,
  operations,
  selectionFor,
  sheetKey,
  type Diagnostic,
  type Entry,
  type Operation,
  type Options,
  type Result,
  type Selection,
  type Spreadsheet,
} from './model';
import './style.css';

const storageKey = 'elonia.datamanager.favorite-sheets.v1';
const folders = [
  { kind: 'input', text: '입력 폴더', icon: FolderOpen },
  { kind: 'code', text: '코드 폴더', icon: Braces },
  { kind: 'data', text: 'bytes 폴더', icon: Archive },
];

function readFavorites(): string[] {
  try {
    const value: unknown = JSON.parse(localStorage.getItem(storageKey) ?? '[]');
    return Array.isArray(value) ? value.filter((v): v is string => typeof v === 'string') : [];
  } catch {
    return [];
  }
}

function App() {
  const [books, setBooks] = useState<Spreadsheet[]>([]);
  const [selected, setSelected] = useState<Selection>({});
  const [validated, setValidated] = useState(new Map<string, string>());
  const [favorites, setFavorites] = useState(readFavorites);
  const [query, setQuery] = useState('');
  const [busy, setBusy] = useState(true);
  const running = useRef(false);
  const [status, setStatus] = useState('목록을 불러오는 중');
  const [logs, setLogs] = useState<Entry[]>([]);
  const [expanded, setExpanded] = useState(false);
  const [focus, setFocus] = useState<Diagnostic>();
  const [toast, setToast] = useState('');
  const [capture, setCapture] = useState(false);
  const captured = useRef(false);
  const app = useRef<HTMLDivElement>(null);
  const search = useRef<HTMLInputElement>(null);
  const selectedRow = useRef<HTMLTableRowElement>(null);

  function recordFailure(action: string, error: unknown) {
    const message = action + ' 실패 · ' + String(error);
    const entry: Entry = {
      time: new Date().toLocaleTimeString('ko-KR', { hour12: false }),
      text: message,
      level: 'error',
    };
    setLogs((old) => [entry, ...old]);
    setStatus(action + ' 실패');
    setExpanded(true);
    setFocus(undefined);
  }

  async function run(operation: Operation, options?: Options) {
    if (running.current) return;
    running.current = true;
    setBusy(true);
    setStatus(operations[operation] + ' 중…');
    try {
      const data = await invoke<Result>('workspace', { operation });
      data.diagnostics.sort(
        (a, b) => Number(a.severity === 'warning') - Number(b.severity === 'warning'),
      );
      const target = options?.problem ? data.diagnostics[options.problem - 1] : undefined;
      setBooks(data.spreadsheets);
      setSelected((old) => selectionFor(data, old, options?.sheet, target));
      if (operation !== 'list') setValidated(lastResults(data));
      else setValidated(new Map());
      const entries = makeLog(operation, data);
      setLogs((old) => [...entries, ...old]);
      setStatus(entries[0].text);
      setFocus(target);
      if (data.diagnostics.length || data.report) setExpanded(true);
    } catch (error) {
      setValidated(new Map());
      recordFailure(operations[operation], error);
    } finally {
      running.current = false;
      setBusy(false);
      if (options?.screenshot) setCapture(true);
    }
  }

  useEffect(() => {
    async function start() {
      try {
        const options = await invoke<Options>('launch_options');
        await run(options.problem ? 'check' : 'list', options);
      } catch (error) {
        recordFailure('시작', error);
        setBusy(false);
      }
    }
    void start();
    function onKey(e: KeyboardEvent) {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'f') {
        e.preventDefault();
        search.current?.focus();
        search.current?.select();
      }
    }
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, []);
  useEffect(() => {
    selectedRow.current?.scrollIntoView({ block: 'nearest' });
  }, [selected]);
  useEffect(() => {
    if (!toast) return;
    const timer = setTimeout(() => setToast(''), 2600);
    return () => clearTimeout(timer);
  }, [toast]);
  useEffect(() => {
    if (!capture || busy || captured.current) return;
    captured.current = true;
    async function screenshot() {
      try {
        const { default: html2canvas } = await import('html2canvas');
        await document.fonts.ready;
        await new Promise<void>((resolve) =>
          requestAnimationFrame(() => requestAnimationFrame(() => resolve())),
        );
        const canvas = await html2canvas(app.current!, { scale: 1, logging: false });
        await invoke('save_capture', { png: canvas.toDataURL('image/png') });
      } catch (e) {
        await invoke('capture_failed', { message: String(e) });
      }
    }
    void screenshot();
  }, [capture, busy]);

  function favorite(key: string) {
    const next = favorites.includes(key) ? favorites.filter((k) => k !== key) : [...favorites, key];
    setFavorites(next);
    try {
      localStorage.setItem(storageKey, JSON.stringify(next));
    } catch {
      setToast('즐겨찾기를 이 기기에 저장할 수 없습니다.');
    }
  }
  function pick(d: Diagnostic) {
    if (books.some((b) => b.name === d.spreadsheet && b.sheets.some((s) => s.name === d.sheet))) {
      setSelected({ book: d.spreadsheet, sheet: d.sheet });
      setQuery('');
    }
    setFocus(d);
  }
  const book = books.find((b) => b.name === selected.book);
  const rows = book
    ? book.sheets
        .filter((s) => s.name.toLocaleLowerCase().includes(query.trim().toLocaleLowerCase()))
        .sort(
          (a, b) =>
            Number(favorites.includes(sheetKey(book.name, b.name))) -
            Number(favorites.includes(sheetKey(book.name, a.name))),
        )
    : [];
  const openBook = () => {
    if (book) void invoke('open_workbook', { name: book.name }).catch((e) => recordFailure('원본 열기', e));
  };

  return (
    <div ref={app} className="manager">
      <header className="topbar">
        <div className="identity">
          <span>
            <Database size={19} />
          </span>
          <div>
            <b>Elonia</b> 데이터 매니저
          </div>
        </div>
        <div className="commands">
          <button disabled={busy} onClick={() => void run('list')}>
            <RefreshCw size={14} />
            새로고침
          </button>
          <button disabled={busy} onClick={() => void run('check')}>
            <CheckCheck size={15} />
            전체 검증
          </button>
          <button disabled={busy} className="primary" onClick={() => void run('build')}>
            <Package size={15} />
            전체 생성
          </button>
        </div>
      </header>
      <div className="workspace">
        <aside>
          <h2>
            스프레드시트<span className="total">{books.length}</span>
          </h2>
          <nav aria-label="스프레드시트 목록">
            {books.map((b) => (
              <button
                key={b.name}
                disabled={busy}
                className={book?.name === b.name ? 'active' : ''}
                aria-current={book?.name === b.name}
                onClick={() => {
                  setSelected({ book: b.name });
                  setQuery('');
                }}
              >
                <FileSpreadsheet size={16} />
                <span>{b.name}</span>
                <small>{b.sheets.length}</small>
              </button>
            ))}
          </nav>
          <section className="folders">
            <h2>작업 폴더</h2>
            {folders.map((f) => (
              <button
                key={f.kind}
                disabled={busy}
                onClick={() =>
                  void invoke('open_folder', { kind: f.kind }).catch((e) =>
                    recordFailure(f.text + ' 열기', e),
                  )
                }
              >
                <f.icon size={15} />
                {f.text}
                <ExternalLink size={11} />
              </button>
            ))}
          </section>
        </aside>
        <main>
          <div className="catalog-heading">
            <div>
              <span className="workbook-label">
                <FileSpreadsheet size={13} />
                {book?.name ?? '스프레드시트'}
              </span>
              <h1>
                시트 목록<span>{book?.sheets.length ?? 0}</span>
              </h1>
            </div>
            <button className="open-book" disabled={busy || !book} onClick={openBook}>
              <ExternalLink size={13} />
              원본 열기
            </button>
          </div>
          <div className="search-wrap">
            <label>
              <Search size={15} />
              <input
                ref={search}
                aria-label="시트 이름 검색"
                placeholder="시트 이름 검색"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Escape') setQuery('');
                }}
              />
              {query && (
                <button
                  aria-label="검색어 지우기"
                  onClick={() => {
                    setQuery('');
                    search.current?.focus();
                  }}
                >
                  <X size={13} />
                </button>
              )}
            </label>
          </div>
          <div className="sheet-list">
            <table>
              <colgroup>
                <col />
                <col className="kind-col" />
                <col className="state-col" />
                <col className="fav-col" />
              </colgroup>
              <thead>
                <tr>
                  <th>시트 이름</th>
                  <th>종류</th>
                  <th>최근 검증 결과</th>
                  <th>
                    <Star size={13} aria-label="즐겨찾기" />
                  </th>
                </tr>
              </thead>
              <tbody>
                {book &&
                  rows.map((sheet) => {
                    const key = sheetKey(book.name, sheet.name);
                    const marked = favorites.includes(key);
                    const result = validated.get(key) ?? '미검증';
                    const verified = result === '통과';
                    const isError = result.startsWith('오류');
                    const warning = result.startsWith('경고') || result === '확인 필요';
                    const ResultIcon = verified
                      ? Check
                      : isError
                        ? CircleAlert
                        : warning
                          ? TriangleAlert
                          : CircleMinus;
                    const SheetIcon =
                      sheet.kind === 'enums'
                        ? List
                        : sheet.kind === 'config'
                          ? Braces
                          : sheet.kind === 'table'
                            ? Table2
                            : Layers2;
                    const active = selected.sheet === sheet.name;
                    function select() {
                      setSelected({ book: book!.name, sheet: sheet.name });
                    }
                    return (
                      <tr
                        key={key}
                        ref={active ? selectedRow : undefined}
                        className={active ? 'selected' : ''}
                        onClick={() => { if (!busy) select(); }}
                      >
                        <td>
                          <button
                            className="pick"
                            disabled={busy}
                            aria-pressed={active}
                            onDoubleClick={openBook}
                          >
                            <SheetIcon size={16} />
                            <span>{sheet.name}</span>
                          </button>
                        </td>
                        <td>{kinds[sheet.kind] ?? sheet.kind}</td>
                        <td>
                          <span
                            className={
                              'result ' + (verified ? 'ok' : isError ? 'error' : warning ? 'warning' : '')
                            }
                          >
                            <ResultIcon size={13} />
                            {result}
                          </span>
                        </td>
                        <td>
                          <button
                            className={'star' + (marked ? ' marked' : '')}
                            disabled={busy}
                            aria-label={sheet.name + ' 즐겨찾기'}
                            aria-pressed={marked}
                            onClick={(e) => {
                              e.stopPropagation();
                              favorite(key);
                            }}
                          >
                            <Star size={15} fill={marked ? 'currentColor' : 'none'} />
                          </button>
                        </td>
                      </tr>
                    );
                  })}
              </tbody>
            </table>
            {!rows.length && (
              <div className="empty">
                <SheetIcon size={23} />
                {busy ? '목록을 불러오는 중…' : query ? '검색 결과가 없습니다.' : '표시할 시트가 없습니다.'}
              </div>
            )}
          </div>
        </main>
      </div>
      <Activity
        entries={logs}
        status={status}
        open={expanded}
        toggle={() => setExpanded((v) => !v)}
        pick={pick}
        focus={focus}
        notify={setToast}
      />
      {toast && (
        <div role="status" className="toast">
          {toast}
        </div>
      )}
    </div>
  );
}
createRoot(document.getElementById('app-root')!).render(<App />);
