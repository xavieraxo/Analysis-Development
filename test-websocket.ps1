# Script simple para probar WebSocket
$uri = "ws://localhost:8093/chat/ws"

# Crear mensaje de prueba
$message = @{
    conversationId = "ws-test-001"
    from = 0
    text = "Necesito crear una API simple para gestionar usuarios"
    at = (Get-Date).ToUniversalTime().ToString("o")
} | ConvertTo-Json

Write-Host "Conectando a WebSocket: $uri" -ForegroundColor Green
Write-Host "Enviando mensaje: $message" -ForegroundColor Yellow
Write-Host ""

# Nota: PowerShell no tiene soporte nativo para WebSocket fácil
# Este es un ejemplo de cómo se usaría con una librería
# Para probar realmente, puedes usar herramientas como:
# - Postman (soporta WebSocket)
# - wscat (npm install -g wscat)
# - Un cliente web simple

Write-Host "Para probar WebSocket, puedes usar:" -ForegroundColor Cyan
Write-Host "1. Postman (tiene soporte WebSocket)" -ForegroundColor White
Write-Host "2. wscat: npm install -g wscat" -ForegroundColor White
Write-Host "   wscat -c ws://localhost:8093/chat/ws" -ForegroundColor White
Write-Host "3. Un cliente web con JavaScript" -ForegroundColor White
Write-Host ""
Write-Host "Mensaje a enviar:" -ForegroundColor Cyan
Write-Host $message -ForegroundColor Gray

