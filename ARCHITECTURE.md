# ARCHITECTURE.md
## Browser + OS Telemetry DLP Architecture (MV3 Extension + .NET Native Host)

> 이 프로젝트는 “웹/SaaS에서 발생하는 유출 행위”를 브라우저 컨텍스트로 탐지하고, OS(.NET) 측 정책 엔진(PDP)에서 결정한 뒤, 브라우저에서 경고/차단(가능 범위)을 수행한다.

---

## 1 핵심 설계 원칙

### 1.1 분리: Detect / Decide / Enforce
- **Detect**: Chrome extension content script가 이벤트를 감지
- **Decide**: .NET AgentCore(PDP)가 policy.json 기반으로 결정
- **Enforce**: extension이 warn/block를 best-effort로 적용

### 1.2 프라이버시/컴플라이언스 가드레일
- **키 입력 전체 수집 금지**
- **원문 텍스트/파일 본문 저장 금지(기본값)**
- 전송/저장은 “시그널(길이, 해시 prefix, 패턴 ID 등)”만
- 최소 권한(permissions/host_permissions 최소화)

### 1.3 감사/증적(Forensics) 품질
- decision에는 반드시 근거가 남아야 함:
  - `policy_id`, `policy_version`, `decision_reason`
- 이벤트는 상관관계 가능하도록:
  - `correlation_id`(탭+시간 기반 MVP)

---

## 2 컴포넌트 개요

### 2.1 Repo 구조(권장)
/extension # MV3 extension (TS)
/agent # .NET native host + agent core + tests
/test-pages # 로컬 재현용 페이지
/docs
/scripts

### 2.2 Component Diagram (Mermaid)
flowchart LR
  subgraph Browser[Chrome / Edge]
    CS[Content Script\n(clipboard/upload/LLM paste)]
    BG[Background SW\n(router + native client + UI trigger)]
    UI[Warn/Block UI\n(banner/notification)]
    CS --> BG
    BG --> UI
    UI --> CS
  end

  subgraph OS[Windows/macOS/Linux - MVP는 Windows 우선]
    NH[Native Messaging Host\n(stdin/stdout framing)]
    CORE[Agent Core\n(PDP + logging + config)]
    LOG[Audit Log\nNDJSON]
    POLICY[policy.json]
    NH --> CORE
    CORE --> LOG
    CORE --> POLICY
  end

  BG <--> NH
## 3데이터 모델

### 3.1 Event (Structured Activity Logging)
이벤트는 원문이 아니라 시그널만 포함한다.
-공통: event_id, timestamp, event_type, url, domain, tab_id, correlation_id
-텍스트 시그널: length, sha256_prefix, patterns[]
-파일 시그널: extension, mime_type, size_bytes(가능하면)

### 3.2 Decision
-decision: allow | warn | block
-policy_id, policy_version
-decision_reason: 감사/사용자 안내에 사용하는 근거 문자열

## 시퀀스 다이어그램

## 4.1 Clipboard Paste -> PDP Decision -> Warn UI
sequenceDiagram
  autonumber
  participant U as User
  participant CS as Content Script
  participant BG as Background SW
  participant NH as Native Host (.NET)
  participant PDP as AgentCore PDP
  participant LOG as NDJSON Log

  U->>CS: Paste into input/textarea
  CS->>CS: Extract text signals (len/hashPrefix/patterns)\nDiscard raw text immediately
  CS->>BG: sendMessage(Event)
  BG->>NH: connectNative + send(Event frame)
  NH->>PDP: Evaluate policy.json
  PDP-->>NH: Decision(warn/block/allow + reason)
  NH->>LOG: Append audit log (event + decision, no raw text)
  NH-->>BG: Decision response frame
  BG-->>CS: enforce(decision)
  CS-->>U: Show banner/notification (warn) or prevent submit (block)

## 4.2 File Upload Attempt -> Warn/Block
sequenceDiagram
  autonumber
  participant U as User
  participant CS as Content Script
  participant BG as Background SW
  participant NH as Native Host
  participant PDP as PDP

  U->>CS: Choose file in <input type=file>
  CS->>CS: Extract file signals (extension/mime/size)\nDo NOT read file content
  CS->>BG: sendMessage(FILE_UPLOAD_ATTEMPT)
  BG->>NH: send(Event)
  NH->>PDP: Evaluate
  PDP-->>NH: Decision
  NH-->>BG: Decision
  BG-->>CS: enforce(decision)
  CS-->>U: warn UI or clear file input / prevent submit (best-effort)

## 4.3 Native Host 연결 실패 시 (Graceful Degradation)
sequenceDiagram
  autonumber
  participant CS as Content Script
  participant BG as Background SW
  participant NH as Native Host

  CS->>BG: sendMessage(Event)
  BG->>NH: connectNative()
  NH-->>BG: fails / not installed
  BG->>BG: Fallback policy\nMVP: allow + local console warn
  BG-->>CS: allow (or warn-only UI)

## 5 정책 엔진(PDP) 평가 로직
### 5.1 우선순위

exceptions[] 매칭 -> 즉시 반환(allow 등)

rules[] priority 내림차순 평가 -> 첫 매칭 반환

default 반환

### 5.2 MVP 매칭 조건

event_type_in

domain_in / domain_not_in

patterns_any

text_length_min

file_extension_in

## 6 보안/위협 모델(간단)
### 6.1 우리가 막고 싶은 것(제품 목표)

외부 도메인/LLM/SaaS로 민감정보가 paste/upload되는 행위에 대한 탐지/경고/차단

### 6.2 우리가 절대 하면 안 되는 것(가드레일)

키로깅 수준의 입력 수집

원문 텍스트/파일 내용 저장

동의 없는 사용자 감시/배포

### 6.3 기술적 공격면 & 완화(MVP 수준)

Native messaging 채널 오남용

allowed_origins에 extension ID만 허용

메시지 폭탄/크래시 유발

프레임 크기 제한, 파싱 예외 처리, 타임아웃

확장 권한 과다

최소 권한 원칙, host_permissions 최소화

## 7 성능 고려사항 (MVP 가이드)

typed input 수집 금지(상시 input 이벤트 수집하지 않음)

paste/submit/file-change 같은 low-frequency 이벤트 중심

중복 알림 디바운싱(같은 탭/도메인에서 연속 paste 시)

이벤트 큐/백프레셔는 추후(프로덕션 하드닝)

## 8 운영/감사 로그 전략

로컬 NDJSON:

한 줄에 하나의 event + decision (또는 분리)

민감 데이터 최소화:

텍스트는 length/hashPrefix/patterns만

결정 근거 보존:

policy_id/version/reason은 필수

## 9 확장 로드맵(프로덕션 지향)

정책 원격 배포/핫리로드

상관관계 고도화(copy -> paste -> upload chain)

SIEM 출력(CEF/JSON syslog)

무결성(서명/해시 체인) 및 로컬 암호화(DPAPI 등)

OS 레벨 텔레메트리(단, 동의/컴플라이언스 전제)