namespace UgadaikaServer.MasterData
{
    /// <summary>
    /// Состояние игрока
    /// </summary>
    public enum PlayerState
    {
        /// <summary>
        /// Новый игрок, только зашел на сервер
        /// </summary>
        New = 0,
        /// <summary>
        /// Готов начать игру
        /// </summary>
        ReadyToStartGame,
        /// <summary>
        /// Отключился от игры
        /// </summary>
        Disconnected,
        /// <summary>
        /// В лобби
        /// </summary>
        InLobby,
        /// <summary>
        /// В игре
        /// </summary>
        InGame
    }
}
