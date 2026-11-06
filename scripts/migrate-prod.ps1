<#
.SYNOPSIS
  Sinh SQL migration idempotent (chạy lại nhiều lần không lỗi) để áp lên database production bằng tay.
.DESCRIPTION
  Mặc định chỉ sinh file scripts/out/migrate-prod.sql (thư mục out/ bị gitignore), rồi in hướng dẫn.
  Cách áp: dán nội dung file vào Supabase SQL Editor, hoặc truyền -ConnectionString (hoặc đặt biến môi trường
  MIGRATE_CONNECTION_STRING) để chạy bằng psql. Connection string dạng URI của Supabase Session pooler:
  postgresql://postgres.<ref>:<mật khẩu>@<host>:5432/postgres?sslmode=require
  Script không chứa secret; không bao giờ commit connection string.
#>
param(
    [string]$OutFile = (Join-Path $PSScriptRoot 'out\migrate-prod.sql'),
    [string]$ConnectionString = $env:MIGRATE_CONNECTION_STRING
)

$ErrorActionPreference = 'Stop'
$backend = Join-Path $PSScriptRoot '..\backend'

New-Item -ItemType Directory -Force -Path (Split-Path $OutFile) | Out-Null

# Biến môi trường ưu tiên hơn user-secrets: lưu giá trị cũ để khôi phục, nếu không dotnet watch run chạy cùng cửa sổ sẽ dùng DB giả.
$overridden = @{
    ASPNETCORE_ENVIRONMENT      = 'Development'
    ConnectionStrings__Default  = 'Host=localhost;Database=unused;Username=unused;Password=unused'
    Jwt__Key                    = 'script-only-dummy-key-not-a-secret-0123456789'
}
$previous = @{}
foreach ($name in $overridden.Keys) {
    $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}

try {
    # dotnet ef khởi động app host nên cần cấu hình hợp lệ; giá trị giả này chỉ dùng cho việc sinh SQL (không kết nối DB).
    foreach ($name in $overridden.Keys) {
        [Environment]::SetEnvironmentVariable($name, $overridden[$name], 'Process')
    }

    dotnet ef migrations script --idempotent `
        --project (Join-Path $backend 'QuizBattle.Infrastructure') `
        --startup-project (Join-Path $backend 'QuizBattle.Api') `
        --output $OutFile
    if ($LASTEXITCODE -ne 0) { throw "dotnet ef thất bại (exit $LASTEXITCODE)" }
    Write-Host "Đã sinh: $OutFile"

    if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
        Write-Host 'Chưa truyền -ConnectionString: dán file SQL trên vào Supabase SQL Editor để áp migration.'
        return
    }

    psql $ConnectionString --set ON_ERROR_STOP=1 --file $OutFile
    if ($LASTEXITCODE -ne 0) { throw "psql thất bại (exit $LASTEXITCODE)" }
    Write-Host 'Đã áp migration.'
}
finally {
    # $null xóa biến khỏi process nếu trước đó không có.
    foreach ($name in $overridden.Keys) {
        [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process')
    }
}
