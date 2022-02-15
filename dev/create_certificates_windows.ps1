# Script for generating and installing the Bravura Safe development certificates on Windows.

$params = @{
    'KeyAlgorithm' = 'RSA';
    'KeyLength' = 4096;
    'NotAfter' = (Get-Date).AddDays(3650);
    'CertStoreLocation' = 'Cert:\CurrentUser\My';
};

$params['Subject'] = 'CN=Bravura Safe Identity Server Dev';
New-SelfSignedCertificate @params;

$params['Subject'] = 'CN=Bravura Safe Data Protection Dev';
New-SelfSignedCertificate @params;
