# 用户数据库重构脚本
# 将混合的用户数据分离为四个独立的数据库文件

param(
    [string]$DataPath = ".\Data",
    [switch]$Backup = $true
)

Write-Host "开始用户数据库重构..." -ForegroundColor Green

# 确保数据目录存在
if (-not (Test-Path $DataPath)) {
    Write-Error "数据目录不存在: $DataPath"
    exit 1
}

# 备份原始文件
if ($Backup) {
    Write-Host "备份原始数据文件..." -ForegroundColor Yellow
    $backupDir = Join-Path $DataPath "backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    
    $filesToBackup = @("users.json", "admins.json", "super_admins.json", "agents.json")
    foreach ($file in $filesToBackup) {
        $sourcePath = Join-Path $DataPath $file
        if (Test-Path $sourcePath) {
            Copy-Item $sourcePath (Join-Path $backupDir $file)
            Write-Host "已备份: $file" -ForegroundColor Gray
        }
    }
}

# 读取现有数据文件
Write-Host "读取现有数据文件..." -ForegroundColor Yellow

$usersPath = Join-Path $DataPath "users.json"
$adminsPath = Join-Path $DataPath "admins.json"
$superAdminsPath = Join-Path $DataPath "super_admins.json"
$agentsPath = Join-Path $DataPath "agents.json"

$allUsers = @()
$allAdmins = @()
$allSuperAdmins = @()
$allAgents = @()

# 读取users.json
if (Test-Path $usersPath) {
    try {
        $usersContent = Get-Content $usersPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($usersContent -is [array]) {
            $allUsers = $usersContent
        } else {
            $allUsers = @($usersContent)
        }
        Write-Host "已读取 users.json: $($allUsers.Count) 条记录" -ForegroundColor Gray
    } catch {
        Write-Warning "读取 users.json 失败: $($_.Exception.Message)"
    }
}

# 读取admins.json
if (Test-Path $adminsPath) {
    try {
        $adminsContent = Get-Content $adminsPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($adminsContent.admins) {
            $allAdmins = $adminsContent.admins
        }
        Write-Host "已读取 admins.json: $($allAdmins.Count) 条记录" -ForegroundColor Gray
    } catch {
        Write-Warning "读取 admins.json 失败: $($_.Exception.Message)"
    }
}

# 读取super_admins.json
if (Test-Path $superAdminsPath) {
    try {
        $superAdminsContent = Get-Content $superAdminsPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($superAdminsContent.superAdmins) {
            $allSuperAdmins = $superAdminsContent.superAdmins
        }
        Write-Host "已读取 super_admins.json: $($allSuperAdmins.Count) 条记录" -ForegroundColor Gray
    } catch {
        Write-Warning "读取 super_admins.json 失败: $($_.Exception.Message)"
    }
}

# 读取agents.json
if (Test-Path $agentsPath) {
    try {
        $agentsContent = Get-Content $agentsPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($agentsContent -is [array]) {
            $allAgents = $agentsContent
        } else {
            $allAgents = @($agentsContent)
        }
        Write-Host "已读取 agents.json: $($allAgents.Count) 条记录" -ForegroundColor Gray
    } catch {
        Write-Warning "读取 agents.json 失败: $($_.Exception.Message)"
    }
}

# 分离数据
Write-Host "分离用户数据..." -ForegroundColor Yellow

# 1. 分离注册用户（仅保留 UserType="user" 且 PermissionLevel=3 的用户）
$registeredUsers = $allUsers | Where-Object { 
    $_.UserType -eq "user" -and $_.PermissionLevel -eq 3 -and -not $_.IsAdmin 
}

# 2. 从users.json中提取普通管理员（PermissionLevel=1或2）
$regularAdminsFromUsers = $allUsers | Where-Object { 
    ($_.UserType -eq "admin" -or $_.IsAdmin -eq $true) -and 
    ($_.PermissionLevel -eq 1 -or $_.PermissionLevel -eq 2)
}

# 3. 从admins.json中提取普通管理员
$regularAdminsFromAdmins = $allAdmins | Where-Object { 
    $_.permissionLevel -eq 1 -or $_.permissionLevel -eq 2 
}

# 4. 合并普通管理员数据并去重
$regularAdmins = @()
foreach ($admin in $regularAdminsFromUsers) {
    $regularAdmins += @{
        id = $admin.Id
        username = if ($admin.Username) { $admin.Username } else { $admin.Phone }
        password = $admin.Password
        name = if ($admin.Name) { $admin.Name } else { $admin.NickName }
        permissionLevel = $admin.PermissionLevel
        isActive = $admin.IsActive
        createdAt = $admin.CreatedAt
        lastLogin = $admin.LastLoginTime
        userType = "regular_admin"
    }
}

foreach ($admin in $regularAdminsFromAdmins) {
    # 检查是否已存在
    $exists = $regularAdmins | Where-Object { $_.username -eq $admin.username }
    if (-not $exists) {
        $regularAdmins += @{
            id = $admin.id
            username = $admin.username
            password = $admin.passwordHash
            name = $admin.name
            permissionLevel = $admin.permissionLevel
            isActive = $true
            createdAt = $admin.createdAt
            lastLogin = $null
            userType = "regular_admin"
        }
    }
}

# 5. 创建新的超级管理员数据（按要求设置两个账号）
$newSuperAdmins = @(
    @{
        id = "super_001"
        username = "superadmin"
        password = "admin"
        name = "主超级管理员"
        userType = "super_admin"
        permissionLevel = 0
        isActive = $true
        createdAt = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
        lastLogin = $null
        securitySettings = @{
            passwordExpires = $false
            twoFactorEnabled = $false
            loginAttemptsLimit = 5
        }
    },
    @{
        id = "super_002"
        username = "xiaolijie2"
        password = "xiaolijie1"
        name = "备用超级管理员"
        userType = "super_admin"
        permissionLevel = 0
        isActive = $true
        createdAt = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
        lastLogin = $null
        securitySettings = @{
            passwordExpires = $false
            twoFactorEnabled = $false
            loginAttemptsLimit = 5
        }
    }
)

# 输出统计信息
Write-Host "数据分离完成:" -ForegroundColor Green
Write-Host "  注册用户: $($registeredUsers.Count) 条" -ForegroundColor Cyan
Write-Host "  业务员: $($allAgents.Count) 条" -ForegroundColor Cyan
Write-Host "  普通管理员: $($regularAdmins.Count) 条" -ForegroundColor Cyan
Write-Host "  超级管理员: $($newSuperAdmins.Count) 条" -ForegroundColor Cyan

# 写入新的数据文件
Write-Host "写入新的数据文件..." -ForegroundColor Yellow

# 1. 更新 users.json（仅包含注册用户）
$newUsersPath = Join-Path $DataPath "users.json"
$registeredUsers | ConvertTo-Json -Depth 10 | Out-File $newUsersPath -Encoding UTF8
Write-Host "已更新: users.json" -ForegroundColor Green

# 2. 保持 agents.json 不变（如果需要的话）
# agents.json 已经是独立的，不需要修改

# 3. 创建 regular_admins.json
$regularAdminsPath = Join-Path $DataPath "regular_admins.json"
$regularAdminsData = @{
    admins = $regularAdmins
    permissionLevels = @{
        "1" = @{
            name = "高级管理员"
            description = "拥有大部分管理权限，不能管理其他管理员"
            permissions = @("user_management", "order_management", "system_settings")
        }
        "2" = @{
            name = "普通管理员"
            description = "拥有基础管理权限"
            permissions = @("user_management", "order_management")
        }
    }
    metadata = @{
        version = "1.0"
        lastUpdated = (Get-Date -Format "yyyy-MM-ddTHH:mm:ss.fffffffK")
        totalCount = $regularAdmins.Count
    }
}
$regularAdminsData | ConvertTo-Json -Depth 10 | Out-File $regularAdminsPath -Encoding UTF8
Write-Host "已创建: regular_admins.json" -ForegroundColor Green

# 4. 更新 super_admins.json
$superAdminsData = @{
    superAdmins = $newSuperAdmins
    metadata = @{
        version = "1.0"
        lastUpdated = (Get-Date -Format "yyyy-MM-ddTHH:mm:ss.fffffffK")
        totalCount = $newSuperAdmins.Count
    }
}
$superAdminsData | ConvertTo-Json -Depth 10 | Out-File $superAdminsPath -Encoding UTF8
Write-Host "已更新: super_admins.json" -ForegroundColor Green

Write-Host "用户数据库重构完成!" -ForegroundColor Green
Write-Host "新的数据库结构:" -ForegroundColor Yellow
Write-Host "  - users.json: 注册用户数据" -ForegroundColor White
Write-Host "  - agents.json: 业务员数据" -ForegroundColor White
Write-Host "  - regular_admins.json: 普通管理员数据" -ForegroundColor White
Write-Host "  - super_admins.json: 超级管理员数据" -ForegroundColor White

if ($Backup) {
    Write-Host "原始数据已备份到: $backupDir" -ForegroundColor Gray
}