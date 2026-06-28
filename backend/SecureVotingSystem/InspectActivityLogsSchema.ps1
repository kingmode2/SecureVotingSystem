Add-Type -AssemblyName System.Data
$conn = 'Server=localhost;Database=SecureVotingDB;Trusted_Connection=True;TrustServerCertificate=True;'
$cnn = New-Object System.Data.SqlClient.SqlConnection($conn)
$cnn.Open()
$cmd = $cnn.CreateCommand()
$cmd.CommandText = "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ActivityLogs'"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Output ($reader.GetString(0) + '|' + $reader.GetString(1) + '|' + $reader.GetString(2))
}
$reader.Close()
$cnn.Close()
