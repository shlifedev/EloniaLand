import { AlertCircle, AlertTriangle, Check, ChevronDown, ChevronUp, Copy, Terminal } from 'lucide-react';
import { useEffect, useRef } from 'react';
import type { Entry, Diagnostic } from './model';

function copy(text: string) {
  // WebKit의 클립보드 권한은 호스트 설정에 따라 달라진다. 클릭 이벤트 안에서 먼저 복사한다.
  const focused = document.activeElement;
  const field = document.createElement('textarea');
  field.value = text;
  field.style.cssText = 'position:fixed;opacity:0;left:0;top:0';
  document.body.appendChild(field);
  field.select();
  let done = false;
  try {
    done = document.execCommand('copy');
  } catch {
    done = false;
  } finally {
    field.remove();
    if (focused instanceof HTMLElement) focused.focus({ preventScroll: true });
  }
  if (done) return Promise.resolve();
  if (navigator.clipboard) return navigator.clipboard.writeText(text);
  return Promise.reject(new Error('셀 주소를 복사할 수 없습니다.'));
}

interface Props {
  entries: Entry[];
  open: boolean;
  status: string;
  toggle: () => void;
  pick: (d: Diagnostic) => void;
  focus?: Diagnostic;
  notify: (message: string) => void;
}
export default function Activity({ entries, open, status, toggle, pick, focus, notify }: Props) {
  const chosen = useRef<HTMLDivElement>(null);
  const latest = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (open && focus) chosen.current?.scrollIntoView({ block: 'nearest' });
    else if (open) latest.current?.scrollTo(0, 0);
  }, [entries, focus, open]);
  return (
    <section className="activity">
      <button className="activity-toggle" onClick={toggle} aria-expanded={open}>
        <Terminal size={15} />
        <b>작업 로그</b>
        <span className="total">{entries.length}</span>
        <i />
        <span className="current-status" role="status">
          {status}
        </span>
        {open ? <ChevronDown size={15} /> : <ChevronUp size={15} />}
      </button>
      {open && (
        <div ref={latest} className="log-body" role="log">
          {entries.length ? (
            entries.map((entry, index) => {
              const d = entry.diagnostic;
              const address =
                d?.address && d.sheet
                  ? d.address.includes('!')
                    ? d.address
                    : d.sheet + '!' + d.address
                  : undefined;
              return (
                <div
                  key={index}
                  ref={d && d === focus ? chosen : undefined}
                  className={'log-line ' + entry.level + (d && d === focus ? ' focused' : '')}
                >
                  <time>{entry.time}</time>
                  {entry.level === 'error' ? (
                    <AlertCircle size={14} />
                  ) : entry.level === 'warning' ? (
                    <AlertTriangle size={14} />
                  ) : (
                    <Check size={13} />
                  )}
                  <div>
                    {d &&
                      (d.spreadsheet && d.sheet ? (
                        <button className="location" onClick={() => pick(d)}>
                          {d.location}
                        </button>
                      ) : (
                        <b className="plain-location">{d.location}</b>
                      ))}
                    <span>{entry.text}</span>
                    {d?.value != null && (
                      <p>
                        <b>값</b>
                        <code>{d.value || '(빈 값)'}</code>
                      </p>
                    )}
                    {d?.fix && (
                      <p>
                        <b>수정 방법</b>
                        {d.fix}
                      </p>
                    )}
                  </div>
                  {address && (
                    <button
                      className="copy"
                      aria-label={address + ' 주소 복사'}
                      title={address + ' 주소 복사'}
                      onClick={() => {
                        void copy(address).then(
                          () => notify('셀 주소를 복사했습니다.'),
                          (e) => notify(String(e)),
                        );
                      }}
                    >
                      <Copy size={13} />
                      주소 복사
                    </button>
                  )}
                </div>
              );
            })
          ) : (
            <div className="empty">아직 실행한 작업이 없습니다.</div>
          )}
        </div>
      )}
    </section>
  );
}
