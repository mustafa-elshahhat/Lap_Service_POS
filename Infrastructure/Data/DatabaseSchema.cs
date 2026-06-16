using System.Collections.Generic;

namespace AlJohary.ServiceHub.Infrastructure.Data
{
    public static class DatabaseSchema
    {
        public static IEnumerable<string> GetCreateTableStatements()
        {
            return new List<string>
            {
                @"CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    username TEXT UNIQUE NOT NULL,
                    password_hash TEXT NOT NULL,
                    full_name TEXT NOT NULL,
                    role TEXT NOT NULL CHECK (role IN ('admin', 'employee')),
                    employee_id INTEGER NULL,
                    max_discount_percent REAL DEFAULT 10.0,
                    max_markup_percent REAL DEFAULT 20.0,
                    is_active INTEGER DEFAULT 1,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (employee_id) REFERENCES employees(id)
                )",

                @"CREATE TABLE IF NOT EXISTS employees (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    full_name TEXT NOT NULL,
                    phone TEXT,
                    job_title TEXT,
                    base_salary REAL NOT NULL DEFAULT 0,
                    notes TEXT,
                    is_active INTEGER NOT NULL DEFAULT 1,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )",

                @"CREATE TABLE IF NOT EXISTS employee_salary_transactions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    employee_id INTEGER NOT NULL,
                    transaction_type TEXT NOT NULL CHECK(transaction_type IN ('salary', 'deduction')),
                    amount REAL NOT NULL,
                    payment_method TEXT,
                    transaction_date TIMESTAMP NOT NULL,
                    notes TEXT,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(employee_id) REFERENCES employees(id),
                    FOREIGN KEY(created_by) REFERENCES users(id)
                )",

                @"CREATE TABLE IF NOT EXISTS products (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    code TEXT UNIQUE NOT NULL,
                    barcode TEXT,
                    name TEXT NOT NULL,
                    purchase_price REAL NOT NULL DEFAULT 0,
                    selling_price REAL NOT NULL DEFAULT 0,
                    quantity INTEGER NOT NULL DEFAULT 0,
                    min_quantity INTEGER DEFAULT 5,
                    supplier_name TEXT,
                    category TEXT,
                    description TEXT,
                    is_active INTEGER DEFAULT 1,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )",

                @"CREATE TABLE IF NOT EXISTS customers (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    phone TEXT,
                    address TEXT,
                    notes TEXT,
                    total_credit REAL DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )",

                @"CREATE TABLE IF NOT EXISTS sales (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    invoice_number TEXT UNIQUE NOT NULL,
                    sale_type TEXT NOT NULL CHECK (sale_type IN ('cash', 'credit')),
                    customer_id INTEGER,
                    user_id INTEGER NOT NULL,
                    subtotal REAL NOT NULL DEFAULT 0,
                    discount_amount REAL DEFAULT 0,
                    markup_amount REAL DEFAULT 0,
                    total_amount REAL NOT NULL DEFAULT 0,
                    paid_amount REAL DEFAULT 0,
                    remaining_amount REAL DEFAULT 0,
                    profit REAL DEFAULT 0,
                    payment_method TEXT DEFAULT 'نقدي',
                    notes TEXT,
                    sale_date TIMESTAMP,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (customer_id) REFERENCES customers(id),
                    FOREIGN KEY (user_id) REFERENCES users(id)
                )",

                @"CREATE TABLE IF NOT EXISTS sale_items (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    sale_id INTEGER NOT NULL,
                    product_id INTEGER NOT NULL,
                    product_code TEXT NOT NULL,
                    product_name TEXT NOT NULL,
                    quantity INTEGER NOT NULL,
                    unit_purchase_price REAL NOT NULL,
                    unit_selling_price REAL NOT NULL,
                    unit_final_price REAL NOT NULL,
                    discount_amount REAL DEFAULT 0,
                    markup_amount REAL DEFAULT 0,
                    total_price REAL NOT NULL,
                    profit REAL DEFAULT 0,
                    paid_amount REAL DEFAULT 0,
                    remaining_amount REAL DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (sale_id) REFERENCES sales(id) ON DELETE CASCADE,
                    FOREIGN KEY (product_id) REFERENCES products(id)
                )",

                @"CREATE TABLE IF NOT EXISTS sale_payments (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    sale_id INTEGER NOT NULL,
                    payment_method TEXT,
                    amount REAL,
                    payment_date DATETIME,
                    notes TEXT,
                    FOREIGN KEY (sale_id) REFERENCES sales(id) ON DELETE CASCADE
                )",

                @"CREATE TABLE IF NOT EXISTS settings (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    key TEXT UNIQUE NOT NULL,
                    value TEXT,
                    description TEXT,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )",

                @"CREATE TABLE IF NOT EXISTS activity_log (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id INTEGER,
                    action TEXT NOT NULL,
                    table_name TEXT,
                    record_id INTEGER,
                    details TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (user_id) REFERENCES users(id)
                )",

                @"CREATE TABLE IF NOT EXISTS returns (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    return_number TEXT UNIQUE NOT NULL,
                    sale_id INTEGER NOT NULL,
                    customer_id INTEGER,
                    user_id INTEGER NOT NULL,
                    total_amount REAL NOT NULL DEFAULT 0,
                    cash_refund REAL DEFAULT 0,
                    debt_deduction REAL DEFAULT 0,
                    reason TEXT,
                    return_date TIMESTAMP,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (sale_id) REFERENCES sales(id),
                    FOREIGN KEY (customer_id) REFERENCES customers(id),
                    FOREIGN KEY (user_id) REFERENCES users(id)
                )",

                @"CREATE TABLE IF NOT EXISTS return_items (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    return_id INTEGER NOT NULL,
                    sale_item_id INTEGER NOT NULL,
                    product_id INTEGER NOT NULL,
                    product_code TEXT NOT NULL,
                    product_name TEXT NOT NULL,
                    quantity INTEGER NOT NULL,
                    unit_price REAL NOT NULL,
                    total_price REAL NOT NULL,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (return_id) REFERENCES returns(id) ON DELETE CASCADE,
                    FOREIGN KEY (sale_item_id) REFERENCES sale_items(id),
                    FOREIGN KEY (product_id) REFERENCES products(id)
                )",

                @"CREATE TABLE IF NOT EXISTS payment_history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    customer_id INTEGER NOT NULL,
                    sale_id INTEGER,
                    payment_type TEXT NOT NULL CHECK (payment_type IN ('credit', 'return')),
                    amount REAL NOT NULL,
                    balance_before REAL NOT NULL DEFAULT 0,
                    balance_after REAL NOT NULL DEFAULT 0,
                    payment_date TIMESTAMP,
                    received_by INTEGER,
                    notes TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (customer_id) REFERENCES customers(id),
                    FOREIGN KEY (sale_id) REFERENCES sales(id),
                    FOREIGN KEY (received_by) REFERENCES users(id)
                )",

                @"CREATE TABLE IF NOT EXISTS suppliers (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    phone TEXT,
                    address TEXT,
                    notes TEXT,
                    total_debt REAL DEFAULT 0,
                    is_active INTEGER DEFAULT 1,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )",

                @"CREATE TABLE IF NOT EXISTS supplier_transactions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    supplier_id INTEGER NOT NULL,
                    transaction_type TEXT NOT NULL CHECK (transaction_type IN ('purchase', 'payment')),
                    amount REAL NOT NULL,
                    reference_number TEXT,
                    transaction_date DATE NOT NULL,
                    payment_method TEXT,
                    balance_before REAL NOT NULL DEFAULT 0,
                    balance_after REAL NOT NULL DEFAULT 0,
                    notes TEXT,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (supplier_id) REFERENCES suppliers(id),
                    FOREIGN KEY (created_by) REFERENCES users(id)
                )",

                @"CREATE TABLE IF NOT EXISTS expenses (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    description TEXT NOT NULL,
                    amount REAL NOT NULL,
                    category TEXT,
                    payment_method TEXT DEFAULT 'نقدي',
                    expense_date TIMESTAMP,
                    user_id INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (user_id) REFERENCES users(id)
                )",

                @"CREATE TABLE IF NOT EXISTS repair_orders (
                    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    order_number        TEXT UNIQUE NOT NULL,
                    customer_id         INTEGER,
                    customer_name       TEXT,
                    customer_phone      TEXT,
                    technician_name     TEXT,
                    user_id             INTEGER NOT NULL,
                    total_amount        REAL NOT NULL DEFAULT 0,
                    paid_amount         REAL NOT NULL DEFAULT 0,
                    remaining_amount    REAL NOT NULL DEFAULT 0,
                    order_status        TEXT NOT NULL DEFAULT 'received',
                    expected_delivery   DATE,
                    intake_date         TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    delivery_date       TIMESTAMP,
                    notes               TEXT,
                    created_at          TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at          TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (customer_id) REFERENCES customers(id),
                    FOREIGN KEY (user_id)     REFERENCES users(id)
                )",

                @"CREATE TABLE IF NOT EXISTS repair_devices (
                    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    order_id            INTEGER NOT NULL,
                    device_type         TEXT NOT NULL DEFAULT 'laptop',
                    brand               TEXT,
                    model               TEXT,
                    serial_number       TEXT,
                    condition           TEXT,
                    reported_issue      TEXT NOT NULL,
                    accessories         TEXT,
                    estimated_cost      REAL DEFAULT 0,
                    service_cost        REAL DEFAULT 0,
                    labor_cost          REAL DEFAULT 0,
                    device_status       TEXT NOT NULL DEFAULT 'received',
                    diagnosis_notes     TEXT,
                    repair_notes        TEXT,
                    created_at          TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (order_id) REFERENCES repair_orders(id) ON DELETE CASCADE
                )",

                @"CREATE TABLE IF NOT EXISTS repair_parts (
                    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    device_id           INTEGER NOT NULL,
                    order_id            INTEGER NOT NULL,
                    product_id          INTEGER,
                    part_name           TEXT NOT NULL,
                    quantity            INTEGER NOT NULL DEFAULT 1,
                    purchase_cost       REAL NOT NULL DEFAULT 0,
                    unit_cost           REAL NOT NULL DEFAULT 0,
                    total_cost          REAL NOT NULL DEFAULT 0,
                    is_from_inventory   INTEGER NOT NULL DEFAULT 0,
                    created_at          TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (device_id)  REFERENCES repair_devices(id) ON DELETE CASCADE,
                    FOREIGN KEY (order_id)   REFERENCES repair_orders(id)  ON DELETE CASCADE,
                    FOREIGN KEY (product_id) REFERENCES products(id)
                )",

                @"CREATE TABLE IF NOT EXISTS repair_payments (
                    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    order_id            INTEGER NOT NULL,
                    amount              REAL NOT NULL,
                    payment_method      TEXT DEFAULT 'نقدي',
                    payment_date        TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    notes               TEXT,
                    user_id             INTEGER,
                    created_at          TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (order_id) REFERENCES repair_orders(id) ON DELETE CASCADE,
                    FOREIGN KEY (user_id)  REFERENCES users(id)
                )",

                "CREATE INDEX IF NOT EXISTS idx_products_code ON products(code)",
                "CREATE INDEX IF NOT EXISTS idx_products_barcode ON products(barcode)",
                "CREATE INDEX IF NOT EXISTS idx_products_name ON products(name)",
                "CREATE INDEX IF NOT EXISTS idx_sales_invoice ON sales(invoice_number)",
                "CREATE INDEX IF NOT EXISTS idx_sales_date ON sales(sale_date)",
                "CREATE INDEX IF NOT EXISTS idx_sales_customer ON sales(customer_id)",

                "CREATE INDEX IF NOT EXISTS idx_customers_phone ON customers(phone)",
                "CREATE INDEX IF NOT EXISTS idx_suppliers_name ON suppliers(name)",
                "CREATE INDEX IF NOT EXISTS idx_supplier_transactions_supplier ON supplier_transactions(supplier_id)",
                "CREATE INDEX IF NOT EXISTS idx_supplier_transactions_date ON supplier_transactions(transaction_date)",
                "CREATE INDEX IF NOT EXISTS idx_employees_name ON employees(full_name)",
                "CREATE INDEX IF NOT EXISTS idx_employee_salary_transactions_employee ON employee_salary_transactions(employee_id)",
                "CREATE INDEX IF NOT EXISTS idx_employee_salary_transactions_date ON employee_salary_transactions(transaction_date)",
                "CREATE INDEX IF NOT EXISTS idx_employee_salary_transactions_type ON employee_salary_transactions(transaction_type)",
                "CREATE UNIQUE INDEX IF NOT EXISTS idx_users_active_employee ON users(employee_id) WHERE employee_id IS NOT NULL AND is_active = 1",
                "CREATE INDEX IF NOT EXISTS idx_expenses_date ON expenses(expense_date)",
                "CREATE INDEX IF NOT EXISTS idx_expenses_category ON expenses(category)",
                "CREATE INDEX IF NOT EXISTS idx_repair_orders_intake ON repair_orders(intake_date)",
                "CREATE INDEX IF NOT EXISTS idx_repair_orders_status ON repair_orders(order_status)",
                "CREATE INDEX IF NOT EXISTS idx_repair_orders_customer ON repair_orders(customer_id)",
                "CREATE INDEX IF NOT EXISTS idx_repair_devices_order ON repair_devices(order_id)",
                "CREATE INDEX IF NOT EXISTS idx_repair_parts_device ON repair_parts(device_id)",
                "CREATE INDEX IF NOT EXISTS idx_repair_parts_order ON repair_parts(order_id)",
                "CREATE INDEX IF NOT EXISTS idx_repair_payments_order ON repair_payments(order_id)",
            };
        }
    }
}
