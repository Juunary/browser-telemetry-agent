# MILESTONES.md
## 마일스톤 (Phase별 목표/완료 기준)

> 원칙: “수집(Extension) - 판정(PDP) - 집행(PEP)” 분리  
> 가드레일: **키 입력 전체 수집 금지**, **원문 저장 금지 기본값**, 최소 권한, 명시적 동의 전제

---

## Milestone 0 — Project Bootstrap
**기간 감각(참고)**: 0.5~1일  
**포함 이슈**: 01, 02, 09

**목표**
- 누구든 레포를 클론 후 빌드/실행 진입 가능
- extension/agent 각각 “hello world” 수준 동작

**완료 기준 (DoD)**
- `scripts/build.*` 실행 시 전체 빌드 성공
- 확장 로드 가능 + .NET 솔루션 빌드/테스트 성공

---

## Milestone 1 — Event Pipeline MVP (Detect → Send → Decide → Return)
**포함 이슈**: 03, 04, 05, 08, 10, 11

**목표**
- clipboard paste 이벤트가 발생하면
  - 원문 없이 시그널만 생성
  - native host로 전달
  - policy.json 기반 decision 생성
  - extension으로 decision 반환

**완료 기준 (DoD)**
- `/test-pages/clipboard.html`에서 paste 시:
  - extension → host로 이벤트 전송 로그 확인
  - host가 `decision(warn/allow/block)`를 반환
- raw text가 어디에도 저장/전송되지 않음(코드/로그/파일 점검)

---

## Milestone 2 — Upload + LLM Paste Coverage (Web/SaaS 컨텍스트 확장)
**포함 이슈**: 06, 07, (08 일부 확장), 11 룰 확장

**목표**
- 웹 컨텍스트 기반 “DLP 블라인드 스팟” 커버 범위 확대
  - 파일 업로드 시도 meta 이벤트
  - LLM prompt paste 이벤트(allowlist 도메인)

**완료 기준 (DoD)**
- `/test-pages/file-upload.html`에서 파일 선택 시 이벤트 생성
- `/test-pages/llm-mock.html`에서 paste 시 `LLM_PROMPT_PASTE` 이벤트 생성
- 정책으로 도메인/확장자/패턴에 따른 warn 동작 확인

---

## Milestone 3 — Enforcement MVP (Warn + Best-effort Block)
**포함 이슈**: 13

**목표**
- decision에 따라 사용자에게 명확한 조치 수행
  - warn: UI로 알림
  - block: 가능한 범위에서 제출/업로드 시도 방지

**완료 기준 (DoD)**
- warn 시 사용자에게 “무엇이, 왜” 위험인지 표시
- block 시 테스트 페이지에서 submit이 막힘(완벽 차단 목표 X)
- UX가 과도하게 방해되지 않음(성능/반복 알림 디바운싱)

---

## Milestone 4 — Audit & Quality (Logs + Tests + Docs)
**포함 이슈**: 12, 14, 15

**목표**
- 운영/감사 관점 최소 요건 충족
  - ndjson audit log
  - 재현 가능한 테스트 페이지
  - 개발 셋업 문서

**완료 기준 (DoD)**
- `agent/logs/events-YYYYMMDD.ndjson`에 이벤트/결정이 남음(원문 없음)
- 문서만 보고 “확장 로드 → host 연결 → warn 재현” 가능
- (옵션) 기본 E2E 자동화가 통과

---

## Milestone 5 — Hardening (선택, 프로덕션 지향)
**포함(추천)**
- 메시지 크기 제한/서명/nonce
- 정책 핫리로드/버전 관리
- 백프레셔(큐) + 장애 시 스풀
- 성능 최적화(샘플링/디바운싱)
- SIEM 출력 포맷(Cef/JSON syslog)

**목표**
- 실제 현업 수준 운영 안정성/보안성 강화

**완료 기준 (DoD)**
- 네이티브 호스트 연결 실패/재연결 시 확장 안정
- 대량 이벤트에서도 CPU/메모리 과도 상승 없음
- 감사 로그 무결성/추적성 개선(최소한의 변경 방지 전략)

---