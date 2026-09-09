# Reading Progress API Documentation

## Функционал отслеживания прогресса чтения

### Эндпоинты

#### 1. Обновить прогресс чтения
**POST** `/api/ReadingProgress`
- **Auth**: Required (любой пользователь)
- **Body**:
```json
{
  "bookId": "uuid",
  "currentPage": 150,
  "totalPages": 300
}
```
- **Response**: `ReadingProgressDto`
- Автоматически рассчитывает процент прочитанного

#### 2. Получить мой прогресс
**GET** `/api/ReadingProgress/my`
- **Auth**: Required (любой пользователь)
- **Response**: `ReadingProgressDto[]`
- Возвращает список всех книг с прогрессом, отсортированный по дате последнего чтения

#### 3. Получить прогресс по книге
**GET** `/api/ReadingProgress/book/{bookId}`
- **Auth**: Required (любой пользователь)
- **Response**: `ReadingProgressDto`
- Возвращает прогресс чтения конкретной книги

## Модель ReadingProgressDto

```json
{
  "id": "uuid",
  "userId": "uuid",
  "bookId": "uuid",
  "bookTitle": "Название книги",
  "bookAuthor": "Автор",
  "currentPage": 150,
  "totalPages": 300,
  "progressPercent": 50.00,
  "lastReadAt": "2026-05-06T12:30:00Z"
}
```

## Фронтенд

В профиле пользователя добавлена секция "Прогрес читання" с:
- Названием и автором книги
- Визуальным прогресс-баром
- Процентом и страницами (150/300)
- Датой последнего чтения

## Миграция базы данных

После получения кода выполни:
```bash
dotnet ef database update
```

Это создаст таблицу `reading_progress` в базе данных.
