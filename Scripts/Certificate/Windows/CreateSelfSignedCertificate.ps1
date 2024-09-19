$cn = "localhost"
$notAfter = [DateTime]::Now.AddYears(100)
$notBefore = [DateTime]::Now.AddDays(-1)

New-SelfSignedCertificate –Subject “CN=$cn” -NotAfter $notAfter -NotBefore $notBefore -KeyUsage DigitalSignature