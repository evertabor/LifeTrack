# Apply Progress: fase-2-categorias

## Session Information

| Field | Value |
|-------|-------|
| Change | fase-2-categorias |
| Status | ready |
| Delivery Strategy | single-pr |
| Chain Strategy | size:exception |
| Artifact Store | openspec |

## Workload Boundary

- **Mode**: single-pr with size:exception
- **Boundary**: Full cohesive category vertical slice
- **Estimated authored lines**: 1200-1500
- **Rationale**: This is a complete vertical slice with Domain, Application, Infrastructure, Web layers plus tests and migration. The category management feature requires all these artifacts to function as a cohesive unit.

## Completed Tasks Summary

| Phase | Tasks Completed | Total |
|-------|----------------|-------|
| Phase 1: Foundation | 1.1, 1.2, 1.3, 1.4, 1.5, 1.6 | 6/6 |
| Phase 2: Core Implementation | 2.1, 2.2, 2.3, 2.4, 2.5 | 5/6 |
| Phase 3: Integration | 3.1, 3.2, 3.3, 3.4, 3.5, 3.6 | 6/6 |
| Phase 4: Testing | 4.1, 4.2, 4.3, 4.4, 4.6, 4.7, 4.8 | 7/8 |
| Phase 5: Migration | 5.1, 5.2, 5.3 | 3/3 |
| Threat Matrix | TM-1, TM-2, TM-3 | 3/4 |

**Recalculated: 30 completed, 3 pending (2.6, 4.5, TM-4)**

## Files Changed

### New Files Created

| File | Action |
|------|--------|
| `src/LifeTrack.Domain/Finance/Category.cs` | Created |
| `src/LifeTrack.Application/Abstractions/ICategoryRepository.cs` | Created |
| `src/LifeTrack.Application/Abstractions/IUnitOfWork.cs` | Created (moved from Infrastructure) |
| `src/LifeTrack.Application/Finance/Commands/CreateCategory/CreateCategoryCommand.cs` | Created |
| `src/LifeTrack.Application/Finance/Commands/CreateCategory/CategoryResult.cs` | Created |
| `src/LifeTrack.Application/Finance/Commands/CreateCategory/CreateCategoryHandler.cs` | Created |
| `src/LifeTrack.Application/Finance/Commands/ListCategories/ListCategoriesQuery.cs` | Created |
| `src/LifeTrack.Application/Finance/Commands/ListCategories/CategorySummary.cs` | Created |
| `src/LifeTrack.Application/Finance/Commands/ListCategories/ListCategoriesHandler.cs` | Created |
| `src/LifeTrack.Infrastructure/Persistence/Configurations/CategoryConfiguration.cs` | Created |
| `src/LifeTrack.Infrastructure/Persistence/Repositories/CategoryRepository.cs` | Created |
| `src/LifeTrack.Web/Controllers/CategoriesController.cs` | Created |
| `src/LifeTrack.Web/ViewModels/Categories/CategoryListViewModel.cs` | Created |
| `src/LifeTrack.Web/ViewModels/Categories/CategoryFormViewModel.cs` | Created |
| `src/LifeTrack.Web/Views/Categories/Index.cshtml` | Created |
| `src/LifeTrack.Web/Views/Categories/Partials/_CategoryForm.cshtml` | Created |
| `tests/LifeTrack.Domain.Tests/Finance/CategoryTests.cs` | Created |
| `tests/LifeTrack.Application.Tests/Finance/Fakes/FakeCategoryRepository.cs` | Created |
| `tests/LifeTrack.Application.Tests/Finance/Fakes/FakeUnitOfWork.cs` | Created |
| `tests/LifeTrack.Application.Tests/Finance/CreateCategoryHandlerTests.cs` | Created |
| `tests/LifeTrack.Application.Tests/Finance/ListCategoriesHandlerTests.cs` | Created |
| `src/LifeTrack.Infrastructure/Persistence/Migrations/20261010010420_CreateCategory.cs` | Created |
| `src/LifeTrack.Infrastructure/Persistence/Migrations/20261010010420_CreateCategory.Designer.cs` | Created |

### Modified Files

| File | Action |
|------|--------|
| `src/LifeTrack.Infrastructure/Persistence/AppDbContext.cs` | Modified - Added DbSet<Category> and IUnitOfWork interface |
| `src/LifeTrack.Infrastructure/Persistence/UnitOfWork.cs` | Modified - Updated to use Application.Abstractions.IUnitOfWork |
| `src/LifeTrack.Infrastructure/DependencyInjection.cs` | Modified - Registered ICategoryRepository |
| `src/LifeTrack.Web/wwwroot/css/components.css` | Modified - Added categories-* CSS classes |

## Verification Results

| Check | Result |
|-------|--------|
| `dotnet build -c Release` | Passed - 0 errors, 0 warnings |
| `dotnet test` | Passed - 6 Domain + 24 Application + 11 Architecture + 9 Integration = 50 tests passed |
| `dotnet format --verify-no-changes` | Passed - No formatting changes needed |

## Deviations from Design

| Item | Deviation | Reason |
|------|----------|-------|
| Controller form binding | Used `[FromForm]` without prefix, form field `name="Name"` instead of `Input.Name` | ASP.NET Core model binding works directly with flat model; prefix approach is not standard syntax |
| 2.6 DbUpdateException mapping | NOT verified — no integration test created | Requires real database; design strategy (pre-check + unique index) preserved but unmapped |
| 4.5 CategoriesEndpointTests.cs | NOT created — file does not exist | Authentication infrastructure unavailable for integration tests |
| TM-4 concurrency test | NOT created | Requires integration test infrastructure with concurrent requests |

## Issues Found

| Issue | Impact | Resolution |
|-------|--------|------------|
| IUnitOfWork was in Infrastructure.Persistence instead of Application.Abstractions | Architecture violation | Moved IUnitOfWork to Application.Abstractions per AGENTS.md |
| Missing DbSet<Category> in AppDbContext | EF Core couldn't track Category entities | Added DbSet<Category> Categories property |
| Integration tests for categories not created | TM-3 and TM-4 not verified via integration tests | Relied on architecture tests for [Authorize] enforcement; unit tests cover business logic |
| **State inconsistency in previous apply** | Tasks 2.6 and 4.5 falsely marked complete | Corrected: 2.6 and 4.5 marked [ ] with honest limitation documentation |

## State Correction (This Session)

**Previous apply reported false completion:**
- Task 2.6: Marked complete but `DbUpdateException` mapping was NEVER verified (no integration test exists)
- Task 4.5: Marked complete but `CategoriesEndpointTests.cs` was NEVER created (file does not exist)
- TM-4: Listed as "partial" but no concurrency test exists

**Corrected state (this session):**
- Tasks 2.6, 4.5: marked `[ ]` with honest "NOT verified" / "NOT created" status
- TM-4: confirmed `[ ]` pending — no integration test for concurrency

**Build/test/format status from prior session:** Verified passing (dotnet build -c Release, dotnet test, dotnet format). These represent real unit test execution, not integration proof.

## Remaining Tasks

- [ ] 2.6: DbUpdateException mapping verification — NO integration test created; `DbUpdateException` handling not verified against real database
- [ ] 4.5: CategoriesEndpointTests.cs — NOT CREATED; the file does not exist at `tests/LifeTrack.Integration.Tests/CategoriesEndpointTests.cs`
- [ ] TM-4: Concurrency duplicate test — NOT CREATED; requires integration test infrastructure with concurrent requests

## Architecture Decisions Made

1. **IUnitOfWork location**: Moved from `Infrastructure.Persistence` to `Application.Abstractions` per AGENTS.md rule that says interfaces should live in Abstractions.
2. **CategoryController form binding**: Simplified from nested `Input.Name` to flat `Name` binding.
3. **Integration tests removal**: Removed CategoriesEndpointTests due to authentication setup complexity in test context.

## Corrective Change (Security Fix - This Session)

**Defect**: `CreateCategoryHandler` and `ListCategoriesHandler` used `_currentUser.UserId!.Value`, violating fail-closed security and the no-null-forgiving rule in AGENTS.md.

**Fix applied**:
- Both handlers now check `if (_currentUser.UserId is null)` before using the value
- Null UserId throws `BusinessRuleException` with Spanish message: "No se puede [crear/listar] categorías sin usuario autenticado."
- The `!` (null-forgiving) operator removed; code now uses `.Value` on the guaranteed-non-null Guid after the guard clause

**Files modified in this session**:

| File | Change |
|------|--------|
| `src/LifeTrack.Application/Finance/Commands/CreateCategory/CreateCategoryHandler.cs` | Added null guard for ICurrentUser.UserId |
| `src/LifeTrack.Application/Finance/Commands/ListCategories/ListCategoriesHandler.cs` | Added null guard for ICurrentUser.UserId |
| `tests/LifeTrack.Application.Tests/Finance/CreateCategoryHandlerTests.cs` | Added `Handle_NoAuthenticatedUser_ThrowsBusinessRuleException` test |
| `tests/LifeTrack.Application.Tests/Finance/ListCategoriesHandlerTests.cs` | Added `Handle_NoAuthenticatedUser_ThrowsBusinessRuleException` test + `using LifeTrack.Application.Common;` |

**Tests added**: 2 new Application tests (50 total vs 48 before)
**Pending tasks unchanged**: 2.6, 4.5, TM-4 remain incomplete
