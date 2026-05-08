param(
    [string]$BaseUrl = "https://localhost:60485/api",
    [string]$AdminToken,
    [string]$EmployeeAToken,
    [string]$EmployeeBToken,
    [string]$EmployeeAId,
    [string]$EmployeeBId,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "[STEP] $Message" -ForegroundColor Cyan
}

function Write-Pass {
    param([string]$Message)
    Write-Host "[PASS] $Message" -ForegroundColor Green
}

function Write-Fail {
    param([string]$Message)
    Write-Host "[FAIL] $Message" -ForegroundColor Red
}

function Get-TokenClaims {
    param([string]$Jwt)

    if ([string]::IsNullOrWhiteSpace($Jwt)) { return @{} }

    $parts = $Jwt.Split('.')
    if ($parts.Length -lt 2) { return @{} }

    $payload = $parts[1].Replace('-', '+').Replace('_', '/')
    switch ($payload.Length % 4) {
        2 { $payload += '==' }
        3 { $payload += '=' }
    }

    try {
        $json = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($payload))
        return ($json | ConvertFrom-Json -AsHashtable)
    }
    catch {
        return @{}
    }
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Url,
        [string]$Token,
        $Body = $null
    )

    if ($DryRun) {
        Write-Host "DRY-RUN $Method $Url"
        if ($null -ne $Body) {
            ($Body | ConvertTo-Json -Depth 10) | Write-Host
        }
        return $null
    }

    $headers = @{ Authorization = "Bearer $Token" }

    try {
        if ($null -ne $Body) {
            return Invoke-RestMethod -Method $Method -Uri $Url -Headers $headers -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 10)
        }

        return Invoke-RestMethod -Method $Method -Uri $Url -Headers $headers
    }
    catch {
        $detail = $null
        if ($_.Exception.Response -and $_.Exception.Response.GetResponseStream()) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $detail = $reader.ReadToEnd()
            $reader.Close()
        }

        if ([string]::IsNullOrWhiteSpace($detail)) {
            throw
        }

        throw "HTTP call failed for $Method $Url :: $detail"
    }
}

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if ($Condition) {
        Write-Pass $Message
        return $true
    }

    Write-Fail $Message
    return $false
}

if (-not $DryRun) {
    if ([string]::IsNullOrWhiteSpace($AdminToken) -or [string]::IsNullOrWhiteSpace($EmployeeAToken) -or [string]::IsNullOrWhiteSpace($EmployeeBToken) -or [string]::IsNullOrWhiteSpace($EmployeeAId) -or [string]::IsNullOrWhiteSpace($EmployeeBId)) {
        throw "AdminToken, EmployeeAToken, EmployeeBToken, EmployeeAId and EmployeeBId are required unless -DryRun is used."
    }
}

$runId = Get-Date -Format "yyyyMMddHHmmss"
$today = Get-Date
$d1 = $today.AddDays(10).Date
$d2 = $today.AddDays(11).Date
$d3 = $today.AddDays(12).Date
$start = (Get-Date $today.AddDays(1).Date -Format "yyyy-MM-ddTHH:mm:ss.fffZ")
$end = (Get-Date $today.AddDays(40).Date -Format "yyyy-MM-ddTHH:mm:ss.fffZ")

$createdHolidayIds = @()
$mandatoryEmployeeHolidayId = $null
$restrictedEmployeeHolidayId = $null
$mandatoryEmployeeBHolidayId = $null
$restrictedEmployeeBHolidayId = $null
$restrictedAllHolidayId = $null
$failed = 0
$setupFailed = $false

Write-Step "Preflight token/identity mapping"
$claimsAdmin = Get-TokenClaims $AdminToken
$claimsA = Get-TokenClaims $EmployeeAToken
$claimsB = Get-TokenClaims $EmployeeBToken

if (-not $DryRun) {
    $aUserId = ($claimsA['nameid'], $claimsA['sub'], $claimsA['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] | Where-Object { $_ } | Select-Object -First 1)
    $bUserId = ($claimsB['nameid'], $claimsB['sub'], $claimsB['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] | Where-Object { $_ } | Select-Object -First 1)
    $aRole = ($claimsA['role'], $claimsA['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] | Where-Object { $_ } | Select-Object -First 1)
    $bRole = ($claimsB['role'], $claimsB['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] | Where-Object { $_ } | Select-Object -First 1)
    $aOrg = $claimsA['organizationId']
    $bOrg = $claimsB['organizationId']

    Write-Host "Employee A Token -> userId=$aUserId role=$aRole org=$aOrg expectedEmployeeId=$EmployeeAId" -ForegroundColor Yellow
    Write-Host "Employee B Token -> userId=$bUserId role=$bRole org=$bOrg expectedEmployeeId=$EmployeeBId" -ForegroundColor Yellow

    if ($EmployeeAId -eq $EmployeeBId) {
        Write-Fail "EmployeeAId and EmployeeBId are identical. This invalidates role-flow assertions."
        exit 1
    }
}

Write-Step "Create scoped holidays as admin"

$mandatoryEmployeeReq = @{
    holidayName = "AUTO-MAND-EMP-$runId"
    holidayDate = (Get-Date $d1 -Format "yyyy-MM-ddTHH:mm:ss.fffZ")
    holidayType = "mandatory"
    description = "Automation test: mandatory scoped to one employee"
    isHalfDay = $false
    applicableTo = "employee"
    employeeIds = @($EmployeeAId)
}

$restrictedEmployeeReq = @{
    holidayName = "AUTO-REST-EMP-$runId"
    holidayDate = (Get-Date $d2 -Format "yyyy-MM-ddTHH:mm:ss.fffZ")
    holidayType = "restricted"
    description = "Automation test: restricted scoped to one employee"
    isHalfDay = $false
    applicableTo = "employee"
    employeeIds = @($EmployeeAId)
}

$restrictedAllReq = @{
    holidayName = "AUTO-REST-ALL-$runId"
    holidayDate = (Get-Date $d3 -Format "yyyy-MM-ddTHH:mm:ss.fffZ")
    holidayType = "restricted"
    description = "Automation test: restricted for all"
    isHalfDay = $false
    applicableTo = "all"
}

$mandatoryEmployeeBReq = @{
    holidayName = "AUTO-MAND-EMPB-$runId"
    holidayDate = (Get-Date $d1.AddDays(1) -Format "yyyy-MM-ddTHH:mm:ss.fffZ")
    holidayType = "mandatory"
    description = "Automation test: mandatory scoped to employee B"
    isHalfDay = $false
    applicableTo = "employee"
    employeeIds = @($EmployeeBId)
}

$restrictedEmployeeBReq = @{
    holidayName = "AUTO-REST-EMPB-$runId"
    holidayDate = (Get-Date $d2.AddDays(1) -Format "yyyy-MM-ddTHH:mm:ss.fffZ")
    holidayType = "restricted"
    description = "Automation test: restricted scoped to employee B"
    isHalfDay = $false
    applicableTo = "employee"
    employeeIds = @($EmployeeBId)
}

try {
    $r1 = Invoke-Api -Method "POST" -Url "$BaseUrl/Holiday" -Token $AdminToken -Body $mandatoryEmployeeReq
    if (-not $DryRun) {
        if (-not $r1.success) { throw "Mandatory employee-scoped holiday creation returned success=false: $($r1.message)" }
        $mandatoryEmployeeHolidayId = $r1.data.holidayId
        $createdHolidayIds += $mandatoryEmployeeHolidayId
        Write-Pass "Created mandatory employee-scoped holiday"
    }
}
catch {
    Write-Fail "Failed to create mandatory employee-scoped holiday: $($_.Exception.Message)"
    $failed++
    $setupFailed = $true
}

try {
    $r4 = Invoke-Api -Method "POST" -Url "$BaseUrl/Holiday" -Token $AdminToken -Body $mandatoryEmployeeBReq
    if (-not $DryRun) {
        if (-not $r4.success) { throw "Mandatory employee-B-scoped holiday creation returned success=false: $($r4.message)" }
        $mandatoryEmployeeBHolidayId = $r4.data.holidayId
        $createdHolidayIds += $mandatoryEmployeeBHolidayId
        Write-Pass "Created mandatory employee-B-scoped holiday"
    }
}
catch {
    Write-Fail "Failed to create mandatory employee-B-scoped holiday: $($_.Exception.Message)"
    $failed++
    $setupFailed = $true
}

try {
    $r5 = Invoke-Api -Method "POST" -Url "$BaseUrl/Holiday" -Token $AdminToken -Body $restrictedEmployeeBReq
    if (-not $DryRun) {
        if (-not $r5.success) { throw "Restricted employee-B-scoped holiday creation returned success=false: $($r5.message)" }
        $restrictedEmployeeBHolidayId = $r5.data.holidayId
        $createdHolidayIds += $restrictedEmployeeBHolidayId
        Write-Pass "Created restricted employee-B-scoped holiday"
    }
}
catch {
    Write-Fail "Failed to create restricted employee-B-scoped holiday: $($_.Exception.Message)"
    $failed++
    $setupFailed = $true
}

try {
    $r2 = Invoke-Api -Method "POST" -Url "$BaseUrl/Holiday" -Token $AdminToken -Body $restrictedEmployeeReq
    if (-not $DryRun) {
        if (-not $r2.success) { throw "Restricted employee-scoped holiday creation returned success=false: $($r2.message)" }
        $restrictedEmployeeHolidayId = $r2.data.holidayId
        $createdHolidayIds += $restrictedEmployeeHolidayId
        Write-Pass "Created restricted employee-scoped holiday"
    }
}
catch {
    Write-Fail "Failed to create restricted employee-scoped holiday: $($_.Exception.Message)"
    $failed++
    $setupFailed = $true
}

try {
    $r3 = Invoke-Api -Method "POST" -Url "$BaseUrl/Holiday" -Token $AdminToken -Body $restrictedAllReq
    if (-not $DryRun) {
        if (-not $r3.success) { throw "Restricted all-scoped holiday creation returned success=false: $($r3.message)" }
        $restrictedAllHolidayId = $r3.data.holidayId
        $createdHolidayIds += $restrictedAllHolidayId
        Write-Pass "Created restricted all-scoped holiday"
    }
}
catch {
    Write-Fail "Failed to create restricted all-scoped holiday: $($_.Exception.Message)"
    $failed++
    $setupFailed = $true
}

if ($setupFailed -and -not $DryRun) {
    Write-Fail "Setup failed. Check token/org alignment and EmployeeAId validity before running assertions."
}

Write-Step "Validate employee-scoped visibility in range/my"
try {
    if ($setupFailed -and -not $DryRun) {
        throw "Skipped: setup failed"
    }

    $aRange = Invoke-Api -Method "GET" -Url "$BaseUrl/Holiday/range/my?start=$start&end=$end" -Token $EmployeeAToken
    $bRange = Invoke-Api -Method "GET" -Url "$BaseUrl/Holiday/range/my?start=$start&end=$end" -Token $EmployeeBToken

    if (-not $DryRun) {
        $aNames = @($aRange.data | ForEach-Object { $_.holidayName })
        $bNames = @($bRange.data | ForEach-Object { $_.holidayName })

        if (-not (Assert-True ($aNames -contains "AUTO-MAND-EMP-$runId") "Employee A sees mandatory employee-scoped holiday")) { $failed++ }
        if (-not (Assert-True (-not ($bNames -contains "AUTO-MAND-EMP-$runId")) "Employee B does NOT see Employee-A mandatory holiday")) { $failed++ }
        if (-not (Assert-True ($bNames -contains "AUTO-MAND-EMPB-$runId") "Employee B sees mandatory employee-B-scoped holiday")) { $failed++ }
        if (-not (Assert-True (-not ($aNames -contains "AUTO-MAND-EMPB-$runId")) "Employee A does NOT see Employee-B mandatory holiday")) { $failed++ }
    }
}
catch {
    Write-Fail "range/my validation failed: $($_.Exception.Message)"
    $failed++
}

Write-Step "Validate employee-scoped restricted availability"
try {
    if ($setupFailed -and -not $DryRun) {
        throw "Skipped: setup failed"
    }

    $aAvailable = Invoke-Api -Method "GET" -Url "$BaseUrl/Holiday/restricted/available/my" -Token $EmployeeAToken
    $bAvailable = Invoke-Api -Method "GET" -Url "$BaseUrl/Holiday/restricted/available/my" -Token $EmployeeBToken

    if (-not $DryRun) {
        $aAvailNames = @($aAvailable.data | ForEach-Object { $_.holidayName })
        $bAvailNames = @($bAvailable.data | ForEach-Object { $_.holidayName })

        if (-not (Assert-True ($aAvailNames -contains "AUTO-REST-EMP-$runId") "Employee A sees employee-scoped restricted holiday")) { $failed++ }
        if (-not (Assert-True (-not ($bAvailNames -contains "AUTO-REST-EMP-$runId")) "Employee B does NOT see Employee-A restricted holiday")) { $failed++ }
        if (-not (Assert-True ($bAvailNames -contains "AUTO-REST-EMPB-$runId") "Employee B sees employee-B-scoped restricted holiday")) { $failed++ }
        if (-not (Assert-True (-not ($aAvailNames -contains "AUTO-REST-EMPB-$runId")) "Employee A does NOT see Employee-B restricted holiday")) { $failed++ }
        if (-not (Assert-True ($aAvailNames -contains "AUTO-REST-ALL-$runId" -and $bAvailNames -contains "AUTO-REST-ALL-$runId") "Both employees see all-scoped restricted holiday")) { $failed++ }
    }
}
catch {
    Write-Fail "restricted/available/my validation failed: $($_.Exception.Message)"
    $failed++
}

Write-Step "Validate restricted selection authorization"
try {
    if (-not $DryRun) {
        if ($setupFailed) {
            throw "Skipped: setup failed"
        }

        # Employee B should fail selecting Employee-A-only restricted holiday
        try {
            $null = Invoke-Api -Method "POST" -Url "$BaseUrl/Holiday/restricted/select" -Token $EmployeeBToken -Body @{ companyHolidayId = $restrictedEmployeeHolidayId }
            Write-Fail "Employee B unexpectedly selected Employee-A restricted holiday"
            $failed++
        }
        catch {
            Write-Pass "Employee B cannot select Employee-A restricted holiday"
        }

        # Employee A should succeed
        $aSelect = Invoke-Api -Method "POST" -Url "$BaseUrl/Holiday/restricted/select" -Token $EmployeeAToken -Body @{ companyHolidayId = $restrictedEmployeeHolidayId }
        if (-not (Assert-True ($aSelect.success) "Employee A can select Employee-A restricted holiday")) { $failed++ }

        # Employee B should succeed on B-scoped restricted holiday
        $bSelect = Invoke-Api -Method "POST" -Url "$BaseUrl/Holiday/restricted/select" -Token $EmployeeBToken -Body @{ companyHolidayId = $restrictedEmployeeBHolidayId }
        if (-not (Assert-True ($bSelect.success) "Employee B can select Employee-B restricted holiday")) { $failed++ }
    }
    else {
        Write-Host "DRY-RUN selection authorization checks"
    }
}
catch {
    Write-Fail "restricted/select validation failed: $($_.Exception.Message)"
    $failed++
}

Write-Step "Cleanup test holidays"
foreach ($id in $createdHolidayIds) {
    try {
        $null = Invoke-Api -Method "DELETE" -Url "$BaseUrl/Holiday/$id" -Token $AdminToken
    }
    catch {
        Write-Fail "Cleanup failed for holiday ${id}: $($_.Exception.Message)"
    }
}

if ($DryRun) {
    Write-Host "DRY-RUN COMPLETE"
    exit 0
}

if ($failed -eq 0) {
    Write-Host "ALL TESTS PASSED" -ForegroundColor Green
    exit 0
}

Write-Host "TESTS FAILED: $failed issue(s)" -ForegroundColor Red
exit 1
