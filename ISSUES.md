# ISSUES.md
## 작업 티켓 (커밋 단위로 구현 가능하게 분해)

> 목적: Chrome Extension(MV3) + .NET Native Messaging Host + Agent Core(PDP/로깅)까지 MVP를 안정적으로 구현한다.
> 가드레일: **키 입력 전체 수집 금지**, **원문 텍스트/파일 본문 저장 금지(기본값)**, 최소 권한, 명시적 동의 전제.

---

## Issue 01 — Repo skeleton + build scripts
**목표**
- 레포 기본 폴더 구조 생성
- extension/agent 각자 빌드/실행 가능한 상태로 스켈레톤 세팅

**작업**
- 폴더 생성:
  - `/extension`, `/agent`, `/docs`, `/test-pages`, `/scripts`
- 루트 `README.md`(간단), `.gitignore`, `editorconfig` 추가
- 빌드 스크립트:
  - `/scripts/build.sh`, `/scripts/build.ps1` (extension build + dotnet build)

**완료 기준**
- `scripts/build.*` 실행 시 extension 빌드 + dotnet 빌드가 모두 성공
- CI 없이 로컬에서 재현 가능

**추천 커밋**
- `chore: init repo skeleton and build scripts`

---

## Issue 02 — Extension toolchain (MV3 + TypeScript + bundler)
**목표**
- MV3 기반 확장 프로그램을 TypeScript로 개발/빌드 가능하게 구성

**작업**
- `extension/package.json` 구성
- bundler 선택 (권장: esbuild 또는 vite)
- `manifest.json`(MV3) 초기 작성
- dist 산출물 생성 및 로드 가능한 형태로 패키징

**완료 기준**
- Chrome(또는 Edge)에서 “압축해제된 확장 프로그램 로드”로 정상 로드됨
- background service worker가 실행 로그를 남김

**추천 커밋**
- `feat(extension): scaffold MV3 + TS build pipeline`

---

## Issue 03 — Shared event schema v1 (TS + C# 동기화)
**목표**
- 이벤트 스키마를 TS와 C#에 각각 정의하여 통신 구조를 고정

**작업**
- `extension/src/shared/schema.ts`
- `agent/src/Dlp.AgentCore/Schema/*.cs`
- 필수 필드/enum 정의:
  - `EventType`(CLIPBOARD_COPY/PASTE, FILE_UPLOAD_ATTEMPT, LLM_PROMPT_PASTE 등)
  - `TextSignals`, `FileSignals`, `PolicyDecision`

**완료 기준**
- 샘플 이벤트 JSON을 TS에서 생성 가능
- C#에서 deserialize 성공(스키마 불일치 없이)

**추천 커밋**
- `feat(schema): add shared event schema v1 (ts + csharp)`

---

## Issue 04 — Pattern detector + hashing (NO raw text persistence)
**목표**
- paste 텍스트를 “메모리에서만” 분석하여 시그널만 생성

**작업**
- `extension/src/shared/patterns.ts`: 정규식 기반 패턴(예: KR_RRN, CREDIT_CARD, AWS_KEY) 최소 세트
- `extension/src/shared/hashing.ts`: sha256 계산 후 prefix만 반환 (예: base64 8 bytes)
- (옵션) `entropy_score` 계산 유틸

**완료 기준**
- 입력 문자열을 넣으면 `{length, sha256_prefix, patterns[]}`가 반환됨
- 디스크/스토리지/로그에 원문이 남지 않음

**추천 커밋**
- `feat(extension): add pattern detection and sha256 prefix hashing`

---

## Issue 05 — Content script: clipboard copy/paste capture
**목표**
- copy/paste 이벤트를 수집해 background로 전달

**작업**
- `document.addEventListener('copy'|'paste')`
- paste 시 clipboard text는 즉시 시그널로 변환 후 폐기
- 이벤트에 url/domain/tab-ish 식별 포함(가능 범위)
- background로 `chrome.runtime.sendMessage()` 전송

**완료 기준**
- `/test-pages/clipboard.html`에서 paste 시 이벤트가 background로 들어옴
- 이벤트 payload에 원문이 없음

**추천 커밋**
- `feat(extension): capture clipboard copy/paste signals`

---

## Issue 06 — Content script: file upload attempt capture
**목표**
- `input[type=file]` 변경 및 업로드 시도를 이벤트로 기록

**작업**
- `change` 이벤트로 file meta 시그널 수집(확장자, mime, size 가능하면)
- submit 감지는 best-effort:
  - form submit 리스너 / 업로드 버튼 클릭 리스너
- 이벤트 타입: `FILE_UPLOAD_ATTEMPT`

**완료 기준**
- `/test-pages/file-upload.html`에서 파일 선택 시 이벤트 발생
- 파일 본문은 절대 읽지 않음 (meta만)

**추천 커밋**
- `feat(extension): detect file upload attempts (metadata only)`

---

## Issue 07 — Content script: LLM prompt paste detector (domain allowlist)
**목표**
- LLM 도메인(테스트 페이지 또는 allowlist)에 한해 prompt paste를 감지

**작업**
- 도메인 allowlist(설정 파일 또는 하드코딩 MVP)
- textarea/contenteditable에 paste될 때 `LLM_PROMPT_PASTE` 이벤트 생성
- 키 입력 전체 수집 금지(typed input은 수집하지 않음)

**완료 기준**
- `/test-pages/llm-mock.html`에서 paste 시 LLM 이벤트로 분리되어 전송됨

**추천 커밋**
- `feat(extension): detect LLM prompt paste on allowlisted domains`

---

## Issue 08 — Background SW: event aggregation + native messaging client
**목표**
- content → background 수신, native host로 전달, decision 수신

**작업**
- background에서 메시지 라우팅:
  - content에서 받은 이벤트를 normalize
  - correlation_id 생성(탭+시간 기반)
- `chrome.runtime.connectNative()`로 host 연결
- 요청/응답 프로토콜 구현(type=event/decision)
- 연결 실패 시 graceful fallback(콘솔 경고 + 이벤트 드롭 또는 로컬 큐는 추후)

**완료 기준**
- host가 없어도 확장이 죽지 않음
- host가 있으면 decision을 받아 content에 전달 가능

**추천 커밋**
- `feat(extension): background native-messaging bridge`

---

## Issue 09 — .NET Solution scaffold (NativeHost + AgentCore + Tests)
**목표**
- .NET 프로젝트 구조 및 기본 실행/테스트 파이프 확보

**작업**
- .NET(권장: .NET 8) 솔루션 생성:
  - `Dlp.NativeHost` (console)
  - `Dlp.AgentCore` (classlib)
  - `Dlp.AgentCore.Tests` (xUnit)
- 공통 유틸(로깅/설정) 최소 셋업

**완료 기준**
- `dotnet build` 성공
- `dotnet test` 성공(placeholder 테스트 1개)

**추천 커밋**
- `feat(agent): scaffold dotnet solution (host/core/tests)`

---

## Issue 10 — Native Messaging framing (stdin/stdout length-prefixed)
**목표**
- Chrome native messaging 표준(4바이트 little-endian length + JSON) 구현

**작업**
- stdin에서 프레임 읽기
- JSON 파싱/검증
- stdout으로 응답 프레임 쓰기
- 잘못된 메시지 방어(크기 제한, try/catch)

**완료 기준**
- 로컬에서 샘플 프레임을 파이프로 넣으면 decision 프레임을 반환
- 크래시 없이 예외 처리

**추천 커밋**
- `feat(agent): implement native messaging framing protocol`

---

## Issue 11 — Policy engine (PDP): policy.json load + eval + decision_reason
**목표**
- 로컬 정책 파일 기반으로 allow/warn/block 결정을 생성

**작업**
- `agent/policy/policy.json` 로딩
- 예외(exceptions) 우선, 이후 rules priority 내림차순
- 매칭 조건:
  - event_type, domain_in/not_in, patterns_any, text_length, file_extension_in
- 결과:
  - policy_id, policy_version, decision, decision_reason

**완료 기준**
- 동일 입력 이벤트에 대해 결정이 안정적으로 동일
- 테스트 케이스 5개 이상으로 검증

**추천 커밋**
- `feat(agent): add PDP policy engine (json rules + priority)`

---

## Issue 12 — Logging: local ndjson audit log (no raw text)
**목표**
- 감사용 이벤트/결정 로그를 로컬에 남김(원문 없음)

**작업**
- `agent/logs/events-YYYYMMDD.ndjson`로 append
- 기본 로그 항목:
  - timestamp, event_id, event_type, domain, decision, policy_id, reason
- 파일 롤링/에러 처리

**완료 기준**
- 이벤트 10개 생성 시 ndjson 10줄 기록
- 원문 텍스트(clipboard 내용)가 기록되지 않음

**추천 커밋**
- `feat(agent): write audit logs as ndjson (no raw content)`

---

## Issue 13 — Enforcement MVP: warn UI + best-effort block
**목표**
- decision을 받아 사용자에게 경고/차단(가능 범위)을 수행

**작업**
- warn:
  - Chrome notification 또는 페이지 내 배너(추천: 페이지 배너)
- block(best-effort):
  - form submit `preventDefault`
  - file input value clear
  - UX 메시지 표시(왜 막았는지)

**완료 기준**
- warn 결정이면 사용자에게 명확히 표시됨
- block 결정이면 테스트 페이지에서 submit이 막힘(완벽 차단 목표 X)

**추천 커밋**
- `feat(extension): enforce warn/block decisions (best-effort)`

---

## Issue 14 — Test pages + basic E2E (optional but 권장)
**목표**
- 로컬에서 재현 가능한 테스트 환경 구축

**작업**
- `/test-pages/clipboard.html`, `/test-pages/file-upload.html`, `/test-pages/llm-mock.html`
- (옵션) Playwright:
  - paste → decision warn 확인
  - file upload change → decision 확인

**완료 기준**
- 브라우저에서 수동 테스트 절차가 README에 정리됨
- (옵션) `npm test`로 기본 시나리오 자동 검증

**추천 커밋**
- `test: add local test pages and basic e2e`

---

## Issue 15 — Packaging + dev setup docs (native host manifest)
**목표**
- 개발자가 따라하면 바로 동작하도록 문서/템플릿 제공

**작업**
- Windows native host manifest 템플릿 추가
- 설치/등록 방법(개발용) 문서화
- troubleshooting(연결 실패, 권한, 경로) 정리

**완료 기준**
- 새 PC에서도 문서만 보고 30분 내 “warn 동작”까지 재현 가능

**추천 커밋**
- `docs: add dev setup for native host manifest and troubleshooting`

---

## 권장 실행 순서
1 → 2 → 3 → 4 → 5 → 8 → 9 → 10 → 11 → 12 → 13 → 14 → 15 → (6,7은 5 이후 병렬 가능)

---
