# SiteSnap DDD 멀티 프로젝트 리팩터링 설계

## 목적

현재 단일 실행 프로젝트와 `MainWindow` partial 클래스에 집중된 업무 규칙, 파일 시스템 처리, 문서 내보내기, UI 갱신 책임을 DDD 계층으로 분리한다. 사용자 기능, 화면 동작, 저장 형식, 파일명, 자동 저장 시점은 변경하지 않는다.

## 선택한 접근

실용적인 멀티 프로젝트 DDD 구조를 적용한다. 폴더만 나누는 방식은 계층 참조를 강제하지 못하고, 모든 UI 이벤트를 한 번에 엄격한 헥사고날 구조로 재작성하는 방식은 회귀 위험이 크다. 프로젝트 경계를 먼저 강제하고 기존 동작을 단계적으로 Application 서비스로 옮긴다.

## 목표 구조

```text
SiteSnap.App (Presentation + Composition Root)
        │
        ├───────────────┐
        ▼               ▼
SiteSnap.Application  SiteSnap.Infrastructure
        │               │
        └───────┬───────┘
                ▼
         SiteSnap.Domain
```

### SiteSnap.Domain

- `AppState`, `PhotoGroup`, `PhotoCell`
- 분류, 셀 이동, 정규화, 페이지 설정 같은 순수 업무 규칙
- 다른 프로젝트를 참조하지 않는다.

### SiteSnap.Application

- 폴더 열기, 저장, 재스캔, 이름 변경, 내보내기 유스케이스
- `IFileScanner`, `IMetadataStore`, `IDocumentExporter` 포트
- 규칙1 파일명 분석과 생성 계획
- Domain만 참조한다.

### SiteSnap.Infrastructure

- 파일 스캔, JSON 저장, DOCX/HWPX 내보내기 구현
- HWPX 기본 리소스와 SkiaSharp 이미지 처리
- Application과 Domain을 참조한다.

### SiteSnap.App

- Avalonia UI와 Composition Root
- 썸네일처럼 Avalonia 타입을 직접 사용하는 표시 전용 서비스
- Application 서비스를 주입받아 사용하며 UI 코드에서 Infrastructure 구현 타입을 직접 사용하지 않는다.

## 이름과 호환성

- 프로젝트와 네임스페이스는 `SiteSnap.Domain`, `SiteSnap.Application`, `SiteSnap.Infrastructure`, `SiteSnap.Presentation`으로 통일한다.
- 실행 프로젝트 경로 `src/SnapSite.App/SiteSnap.App.csproj`는 기존 실행·배포 스크립트 호환성을 위해 유지한다.
- `sitesnape_manifest.json` 이름, JSON 필드, 레거시 마이그레이션, 폴더 depth, UI 문구와 사용자 동작은 유지한다.

## 마이그레이션 순서

1. 현재 기준 빌드와 호출 경로를 확인한다.
2. 솔루션과 Domain/Application/Infrastructure 프로젝트를 만든다.
3. 로직 본문을 바꾸지 않고 Domain 모델과 Infrastructure 구현 및 HWPX 리소스를 이동한다.
4. Application 포트와 유스케이스를 추가하고 Composition Root에서 구현체를 조립한다.
5. `MainWindow`를 Shell, Workspace, Classified, Unclassified, DragDrop, Navigation 책임별 partial 파일로 분리한다.
6. 설정 화면을 Dialog, Form, Preview 파일로 나눈다.
7. `DocumentExporter`를 DOCX, HWPX, 이미지 처리 파일로 나눈다.
8. 문서와 검증 항목을 현재 구조에 맞춘다.

각 단계에서 빌드가 통과한 뒤 다음 단계로 이동한다.

## 회귀 방지

- Domain/Application 특성 테스트를 추가한다.
- 매니페스트 저장·로드 왕복을 검증한다.
- 공종 추가·삭제·순서 변경, 셀 이동, 빈 셀, 규칙1 계획을 검증한다.
- HWPX/DOCX 생성과 HWPX 리소스 포함을 스모크 테스트한다.
- 전체 솔루션 빌드와 테스트를 실행한다.
- 기존 실행 프로젝트와 배포 스크립트 경로를 확인한다.
- 앱을 재시작해 프로세스가 정상 실행되는지 확인한다.

## 비기능 제약

- UI 부분 갱신 레지스트리와 렌더링 우선 자동 저장 방식을 유지한다.
- 파일 이동만으로 끝내지 않고 프로젝트 참조 방향으로 계층 규칙을 강제한다.
- 리팩터링 중 기능 개선이나 UI 변경을 함께 수행하지 않는다.
