-- SQL скрипт для додавання 5 тестових книг
-- Виконай цей скрипт в MySQL Workbench або через командний рядок

USE library_db;

-- Перевірка чи існує жанр "Класика"
INSERT INTO genres (id, name) VALUES
('550e8400-e29b-41d4-a716-446655440001', 'Класика')
ON DUPLICATE KEY UPDATE name = name;

-- Додавання книг
INSERT INTO books (id, title, author, isbn, genre_id, total_copies, available_copies, pdf_url, rating, is_active, created_at, updated_at)
VALUES
-- 1. Кобзар - Тарас Шевченко
(UUID(), 'Кобзар', 'Тарас Шевченко', '978-966-03-4567-1', '550e8400-e29b-41d4-a716-446655440001', 3, 3, '/books/kobzar.pdf', 4.9, 1, NOW(), NOW()),

-- 2. Маленький принц - Антуан де Сент-Екзюпері
(UUID(), 'Маленький принц', 'Антуан де Сент-Екзюпері', '978-617-09-2345-8', '550e8400-e29b-41d4-a716-446655440001', 5, 5, '/books/prince.pdf', 4.8, 1, NOW(), NOW()),

-- 3. 1984 - Джордж Орвелл
(UUID(), '1984', 'Джордж Орвелл', '978-0-452-28423-4', '550e8400-e29b-41d4-a716-446655440001', 4, 4, '/books/1984.pdf', 4.7, 1, NOW(), NOW()),

-- 4. Ферма тварин - Джордж Орвелл
(UUID(), 'Ферма тварин', 'Джордж Орвелл', '978-0-452-28424-1', '550e8400-e29b-41d4-a716-446655440001', 3, 3, '/books/animal-farm.pdf', 4.6, 1, NOW(), NOW()),

-- 5. Гордість і упередження - Джейн Остін
(UUID(), 'Гордість і упередження', 'Джейн Остін', '978-0-141-43951-8', '550e8400-e29b-41d4-a716-446655440001', 4, 4, '/books/pride-and-prejudice.pdf', 4.8, 1, NOW(), NOW());

-- Перевірка результату
SELECT id, title, author, pdf_url, rating FROM books ORDER BY created_at DESC LIMIT 5;
