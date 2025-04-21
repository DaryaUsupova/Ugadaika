using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UgadaikaClientWpf.Settings;

namespace UgadaikaClientWpf.Windows
{
    /// <summary>
    /// Игровое окно - отображает состояние текущей игры
    /// (замаскированное слово, подсказку, список игроков и использованные буквы)
    /// </summary>
    public partial class GameWindow : UserControl
    {
        /// <summary>
        /// Конфиг
        /// </summary>
        private readonly ServiceConfig _config;

        /// <summary>
        /// Использованные символы
        /// </summary>
        public List<char> UsedChars = [];
        /// "serviceConfig"Конфигурация сервера
        /// "players" - Список имен игроков
        ///"wordDescription"Подсказка к загаданному слову
        /// starredWord - Замаскированное слово (например "****")
        public GameWindow(ServiceConfig serviceConfig, List<string> players, string wordDescription,
            string starredWord)
        {
            _config = serviceConfig;
            InitializeComponent();
            PlayersInfo.Text = String.Join(Environment.NewLine, players.Select(p => $"{p}: 0"));// Каждый игрок отображается в формате "Имя: 0"
            StarredWord.Text = starredWord; // Устанавливаем замаскированное слово
            Desciption.Text = wordDescription; // Устанавливаем подсказку к слову
        }
    }
}
