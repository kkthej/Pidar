-- Create database if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_database WHERE datname = 'pidar_db') THEN
        CREATE DATABASE pidar_db
            WITH 
            OWNER = postgres
            ENCODING = 'UTF8'
            LC_COLLATE = 'en_US.utf8'
            LC_CTYPE = 'en_US.utf8'
            TABLESPACE = pg_default
            CONNECTION LIMIT = -1;
    END IF;
END $$;

-- Add comment to database
COMMENT ON DATABASE pidar_db IS 'Main database for PIDAR application';