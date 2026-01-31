---
description: Quick commit changes to the repository
---

# Commit Workflow

Quick workflow for committing changes to the repository with auto-merge support.

## Steps

### 1. Check Status

// turbo

```powershell
git status
```

Review the changed files and ensure only intended files are modified.

### 2. Stage Changes

For specific files:

```powershell
git add <file1> <file2>
```

For all changes:
// turbo

```powershell
git add .
```

### 3. Commit with Message

Use conventional commit format:

- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `style:` - Code style changes (formatting, etc.)
- `refactor:` - Code refactoring
- `test:` - Adding or updating tests
- `chore:` - Maintenance tasks

```powershell
git commit -m "type: description"
```

Examples:

```powershell
git commit -m "feat: add auto-rejoin functionality"
git commit -m "fix: resolve sub-place launch loop"
git commit -m "docs: update README with new features"
```

### 4. Push to Feature Branch (if main is protected)

If pushing to `main` fails due to branch protection:

```powershell
# Create and switch to feature branch
git checkout -b feat/your-feature-name

# Push to feature branch
git push -u origin feat/your-feature-name

# Create PR with auto-merge enabled
# Create PR
gh pr create --fill

# Enable Auto-Merge & Auto-Delete Branch
gh pr merge --auto --squash --delete-branch
```

The `--auto-merge` flag will automatically merge the PR once all checks pass.
The `--delete-branch` flag will automatically delete the branch after merge.

### 5. Direct Push (if no branch protection)

// turbo

```powershell
git push origin main
```

## Quick Commands

### Create PR with Auto-Merge

```powershell
gh pr create --fill
gh pr merge --auto --squash --delete-branch
```

### Amend Last Commit

```powershell
git commit --amend -m "new message"
git push --force origin feat/branch-name
```

### Undo Last Commit (keep changes)

```powershell
git reset --soft HEAD~1
```

### View Commit History

// turbo

```powershell
git log --oneline -10
```

## Auto-Merge Notes

- Auto-merge requires all status checks to pass
- Branch will be automatically deleted after successful merge
- You can monitor the merge status with: `gh pr status`
