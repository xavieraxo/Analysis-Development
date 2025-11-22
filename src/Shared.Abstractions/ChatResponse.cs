namespace Shared.Abstractions;

/// <summary>
/// Respuesta del orquestador que incluye el resumen final para el usuario
/// y todas las conversaciones internas para el PDF
/// </summary>
public record ChatResponse(
    /// <summary>
    /// Resumen final del MVP consolidado para mostrar en el chat
    /// </summary>
    ChatMessage Summary,
    
    /// <summary>
    /// Todas las conversaciones internas entre agentes (para el PDF)
    /// </summary>
    List<ChatMessage> InternalConversations
);

