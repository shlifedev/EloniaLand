export type Operation = 'list' | 'check' | 'build';
export const operations = { list: '새로고침', check: '전체 검증', build: '전체 생성' };
export const kinds = {
  table: '데이터 표',
  config: '설정 표',
  childRows: '행 확장',
  columnExtension: '열 확장',
  enums: '열거형',
  ignored: '제외',
  invalid: '잘못된 이름',
};
export interface Sheet {
  name: string;
  kind: keyof typeof kinds;
}
export interface Spreadsheet {
  name: string;
  sheets: Sheet[];
}
export interface Diagnostic {
  severity: 'error' | 'warning';
  location: string;
  spreadsheet?: string;
  sheet?: string;
  address?: string;
  value?: string;
  reason: string;
  fix?: string;
}
export interface Result {
  spreadsheets: Spreadsheet[];
  diagnostics: Diagnostic[];
  report?: { written: string[]; deleted: string[]; unchanged: number };
}
export interface Options {
  sheet?: string;
  problem?: number;
  screenshot?: boolean;
}
export interface Entry {
  time: string;
  text: string;
  level: 'normal' | 'error' | 'warning';
  diagnostic?: Diagnostic;
}
export interface Selection {
  book?: string;
  sheet?: string;
}

export function sheetKey(book: string, sheet: string) {
  return JSON.stringify([book, sheet]);
}

export function selectionFor(
  result: Result,
  old: Selection,
  sheetName?: string,
  issue?: Diagnostic,
): Selection {
  const requested = issue?.spreadsheet
    ? result.spreadsheets.find((b) => b.name === issue.spreadsheet)
    : sheetName
      ? result.spreadsheets.find((b) => b.sheets.some((s) => s.name === sheetName))
      : undefined;
  const book =
    requested ??
    result.spreadsheets.find((b) => b.name === old.book) ??
    result.spreadsheets.find((b) => b.name === 'Elonia.xlsx') ??
    result.spreadsheets[0];
  const name = issue?.sheet ?? sheetName ?? (book?.name === old.book ? old.sheet : undefined);
  return { book: book?.name, sheet: book?.sheets.find((s) => s.name === name)?.name };
}

export function lastResults(result: Result) {
  const statuses = new Map<string, string>();
  for (const book of result.spreadsheets) {
    const globalError = result.diagnostics.some(
      (d) => d.severity === 'error' && !d.sheet && (!d.spreadsheet || d.spreadsheet === book.name),
    );
    for (const sheet of book.sheets) {
      const issues = result.diagnostics.filter((d) => d.sheet === sheet.name && d.spreadsheet === book.name);
      const errors = issues.filter((d) => d.severity === 'error').length;
      const warnings = issues.length - errors;
      const status = errors
        ? '오류 ' + errors
        : warnings
          ? '경고 ' + warnings
          : sheet.kind === 'ignored'
            ? '대상 제외'
            : globalError
              ? '확인 필요'
              : '통과';
      statuses.set(sheetKey(book.name, sheet.name), status);
    }
  }
  return statuses;
}

export function makeLog(op: Operation, result: Result): Entry[] {
  const time = new Date().toLocaleTimeString('ko-KR', { hour12: false });
  const lines: Entry[] = [];
  const errors = result.diagnostics.filter((d) => d.severity === 'error').length;
  const warnings = result.diagnostics.length - errors;
  const report = result.report;
  const summary =
    op === 'list'
      ? '스프레드시트 ' + result.spreadsheets.length + '개'
      : op === 'build' && report
        ? '기록 ' +
          report.written.length +
          '개 · 삭제 ' +
          report.deleted.length +
          '개 · 동일 ' +
          report.unchanged +
          '개'
        : '오류 ' + errors + '개 · 경고 ' + warnings + '개';
  lines.push({
    time,
    level: errors ? 'error' : warnings ? 'warning' : 'normal',
    text: operations[op] + (errors ? ' 실패' : ' 완료') + ' · ' + summary,
  });
  for (const diagnostic of result.diagnostics)
    lines.push({ time, level: diagnostic.severity, text: diagnostic.reason, diagnostic });
  if (report) {
    for (const path of report.written) lines.push({ time, level: 'normal', text: '기록  ' + path });
    for (const path of report.deleted) lines.push({ time, level: 'normal', text: '삭제  ' + path });
  }
  return lines;
}
