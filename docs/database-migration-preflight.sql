-- Read-only checks. Run against the target MySQL database before applying
-- AddBookValidationConstraints or AddUniqueUserEmailAndBookIsbn.

SELECT 'email longer than 254 characters' AS issue, COUNT(*) AS row_count
FROM users
WHERE CHAR_LENGTH(email) > 254;

SELECT email, COUNT(*) AS row_count
FROM users
GROUP BY email
HAVING COUNT(*) > 1;

SELECT isbn, COUNT(*) AS row_count
FROM books
GROUP BY isbn
HAVING COUNT(*) > 1;

SELECT 'book text exceeds target column length' AS issue, COUNT(*) AS row_count
FROM books
WHERE CHAR_LENGTH(title) > 255
   OR CHAR_LENGTH(author) > 150
   OR CHAR_LENGTH(isbn) > 20
   OR CHAR_LENGTH(source_url) > 1000
   OR CHAR_LENGTH(quote) > 2000
   OR CHAR_LENGTH(image_url) > 1000
   OR CHAR_LENGTH(description) > 5000;

SELECT 'book values violate target check constraints' AS issue, COUNT(*) AS row_count
FROM books
WHERE total_copies NOT BETWEEN 1 AND 1000
   OR available_copies < 0
   OR available_copies > total_copies
   OR (pages IS NOT NULL AND pages NOT BETWEEN 1 AND 100000)
   OR (rating IS NOT NULL AND rating NOT BETWEEN 0 AND 5);