# Cubes

Игра на Unity: стройте башню из разноцветных кубиков, перетаскивайте их с панели и удаляйте через отверстие на игровом поле.

Используемые технологии и плагины:

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3-blue?logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-512BD4)
[![VContainer](https://img.shields.io/badge/DI-VContainer-orange)](https://github.com/hadashiA/VContainer)
[![DOTween](https://img.shields.io/badge/Animation-DOTween-green)](https://dotween.demigiant.com/)
[![Odin Serializer](https://img.shields.io/badge/Serialization-Odin_Serializer-green)](https://github.com/TeamSirenix/odin-serializer)
![Unity Localization](https://img.shields.io/badge/Localization-1.5.9-blue)

## 🎮 Демонстрация

[![▶ Смотреть видео — Cubes](docs/images/gameplay.png)](https://drive.google.com/file/d/1k47r6H8iZYY4xgUfw32F5DlwEB-a7ObB/view?usp=sharing)

[▶ Смотреть видео на Google Drive](https://drive.google.com/file/d/1k47r6H8iZYY4xgUfw32F5DlwEB-a7ObB/view?usp=sharing)

### Управление

Перетащите кубик с нижней панели вверх на жёлтое поле, чтобы начать башню. Следующие кубики отпускайте над кубиками башни. Горизонтальный жест прокручивает панель. Для удаления перетащите кубик из башни в отверстие слева.

Кнопка **EN / RU** меняет язык. Кнопка перезапуска открывает подтверждение сброса.

## 🏗 Архитектурные решения

### Разделение по слоям

Код разделён на пять сборок через Assembly Definition. **Domain** и **Application** не зависят от Unity API, поэтому правила игры можно проверять отдельно от сцены.

| Слой | Ответственность |
| --- | --- |
| **Domain** | Модель башни, типы и цвета кубиков, геометрия поля, ограничения размещения и удаления |
| **Application** | Игровая сессия, правила размещения, снимки состояния, координация сохранения и контракты сервисов |
| **Infrastructure** | Бинарное хранение через Odin Serializer, резервное восстановление, интеграция с Unity Localization |
| **Presentation** | View и Presenter, жесты перетаскивания, интерфейс, анимации DOTween и уведомления |
| **Composition** | Регистрация зависимостей в VContainer, конфигурация игры и запуск сцены |

## 📁 Структура проекта

```text
Assets/
├── _Source/
│   ├── Domain/             # модели и геометрия башни
│   ├── Application/        # игровая сессия, правила и контракты
│   ├── Infrastructure/    # хранение прогресса и локализация
│   ├── Presentation/      # View, Presenter, жесты и анимации
│   └── Composition/       # DI, конфигурация и запуск
├── Content/
│   ├── Art/               # графика игры
│   ├── Configuration/     # ScriptableObject-настройки
│   ├── Fonts/             # игровой шрифт
│   ├── Localization/      # локали и таблицы переводов
│   └── Prefabs/           # кубик и уведомление
├── Scenes/                # Gameplay.unity
├── Plugins/               # DOTween и Odin Serializer
└── Tests/EditMode/        # тесты слоёв приложения
```
