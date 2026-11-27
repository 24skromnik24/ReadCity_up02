
CREATE TABLE roles (
    id SERIAL PRIMARY KEY,
    role_name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE units (
    id SERIAL PRIMARY KEY,
    unit_name VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE categories (
    id SERIAL PRIMARY KEY,
    category_name VARCHAR(150) NOT NULL UNIQUE
);

CREATE TABLE suppliers (
    id SERIAL PRIMARY KEY,
    supplier_name VARCHAR(255) NOT NULL UNIQUE
);

CREATE TABLE manufacturers (
    id SERIAL PRIMARY KEY,
    manufacturer_name VARCHAR(255) NOT NULL UNIQUE
);

CREATE TABLE pvz (
    id SERIAL PRIMARY KEY,
    address TEXT NOT NULL
);

CREATE TABLE statuses (
    id SERIAL PRIMARY KEY,
    status_name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    id_role INT NOT NULL REFERENCES roles(id) ON UPDATE CASCADE ON DELETE RESTRICT,
    last_name VARCHAR(100) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    middle_name VARCHAR(100),
    login VARCHAR(150) NOT NULL UNIQUE,
    password VARCHAR(150) NOT NULL
);

CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    article VARCHAR(50) NOT NULL UNIQUE,
    product_name VARCHAR(255) NOT NULL,
    id_unit INT NOT NULL REFERENCES units(id) ON UPDATE CASCADE ON DELETE RESTRICT,
    price DECIMAL(10, 2) NOT NULL CHECK (price >= 0),
    id_supplier INT NOT NULL REFERENCES suppliers(id) ON UPDATE CASCADE ON DELETE RESTRICT,
    id_manufacturer INT NOT NULL REFERENCES manufacturers(id) ON UPDATE CASCADE ON DELETE RESTRICT,
    id_category INT NOT NULL REFERENCES categories(id) ON UPDATE CASCADE ON DELETE RESTRICT,
    discount INT NOT NULL DEFAULT 0 CHECK (discount >= 0 AND discount <= 100),
    stock_quantity INT NOT NULL DEFAULT 0 CHECK (stock_quantity >= 0),
    description TEXT,
    photo VARCHAR(255)
);

CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    order_date DATE NOT NULL,
    delivery_date DATE NOT NULL,
    id_pickup_point INT NOT NULL REFERENCES pvz(id) ON UPDATE CASCADE ON DELETE RESTRICT,
    id_user INT NOT NULL REFERENCES users(id) ON UPDATE CASCADE ON DELETE RESTRICT,
    pickup_code INT NOT NULL,
    id_status INT NOT NULL REFERENCES statuses(id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE order_items (
    id_order INT NOT NULL REFERENCES orders(id) ON UPDATE CASCADE ON DELETE CASCADE,
    id_product INT NOT NULL REFERENCES products(id) ON UPDATE CASCADE ON DELETE RESTRICT,
    quantity INT NOT NULL CHECK (quantity > 0),
    id SERIAL PRIMARY KEY
);
