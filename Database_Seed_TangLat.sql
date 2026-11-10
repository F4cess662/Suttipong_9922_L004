-- ============================================================================
-- สคริปต์ฐานข้อมูลและข้อมูลตัวอย่าง (Seed Data) สำหรับโปรเจกต์ "ทางลัด (Tang-Lat)"
-- ฐานข้อมูล: csi402db (MySQL / MariaDB)
-- รองรับการ DROP DATABASE เก่าทิ้งแล้วรันสร้างใหม่ทั้งหมดได้ทันที (100% Compatible)
-- ============================================================================

DROP DATABASE IF EXISTS `csi402db`;
CREATE DATABASE IF NOT EXISTS `csi402db` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `csi402db`;

SET FOREIGN_KEY_CHECKS = 0;

-- 0. ตาราง labstudent (สำหรับงานส่งรายสัปดาห์ใน HomeController เพื่อไม่ให้หน้า Lab พัง)
CREATE TABLE IF NOT EXISTS `labstudent` (
  `StdId` varchar(10) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL,
  `StdPassword` varchar(30) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  `StdName` varchar(50) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  `StdLastname` varchar(100) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  PRIMARY KEY (`StdId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 1. ตาราง users
CREATE TABLE IF NOT EXISTS `users` (
  `id` int NOT NULL AUTO_INCREMENT,
  `userName` varchar(50) NOT NULL,
  `email` varchar(100) NOT NULL,
  `password` varchar(255) NOT NULL,
  `phone_number` varchar(20) DEFAULT NULL,
  `role` enum('admin','employee','customer') DEFAULT 'customer',
  `status` enum('active','pending','banned') DEFAULT 'active',
  `last_active` datetime DEFAULT NULL,
  `deleted_at` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `email` (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. ตาราง employees (พร้อมคอลัมน์ salary ที่เพิ่มใหม่)
CREATE TABLE IF NOT EXISTS `employees` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int DEFAULT NULL,
  `first_name` varchar(100) NOT NULL,
  `last_name` varchar(100) NOT NULL,
  `position` varchar(100) DEFAULT NULL,
  `department` varchar(100) DEFAULT NULL,
  `salary` decimal(10,2) DEFAULT 25000.00,
  `hire_date` date DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `user_id` (`user_id`),
  CONSTRAINT `employees_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. ตาราง locations (สถานที่ท่องเที่ยวในกรุงเทพฯ)
CREATE TABLE IF NOT EXISTS `locations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(150) NOT NULL,
  `description` text,
  `address` varchar(255) DEFAULT NULL,
  `latitude` decimal(10,8) NOT NULL,
  `longitude` decimal(11,8) NOT NULL,
  `category` enum('attraction','restaurant','shopping','transit_station','other') DEFAULT 'attraction',
  `image_url` varchar(255) DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT 1,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. ตาราง transport_types (รถเมล์, รถไฟฟ้า, มอเตอร์ไซค์วิน ฯลฯ)
CREATE TABLE IF NOT EXISTS `transport_types` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `type` enum('train','bus','motorcycle','boat','other') NOT NULL,
  `base_fare` decimal(8,2) DEFAULT 0.00,
  `fare_per_km` decimal(8,2) DEFAULT 0.00,
  `fare_per_station` decimal(8,2) DEFAULT 0.00,
  `description` text,
  `is_active` tinyint(1) DEFAULT 1,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. ตาราง user_trips (ทริปของผู้ใช้)
CREATE TABLE IF NOT EXISTS `user_trips` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `trip_name` varchar(150) NOT NULL,
  `total_distance_km` decimal(8,2) DEFAULT 0.00,
  `estimated_cost` decimal(8,2) DEFAULT 0.00,
  `is_favorite` tinyint(1) DEFAULT 0,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `user_trips_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 6. ตาราง trip_locations (จุดแวะพักในแต่ละทริป)
CREATE TABLE IF NOT EXISTS `trip_locations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `trip_id` int NOT NULL,
  `location_id` int NOT NULL,
  `stop_order` int NOT NULL,
  `transport_id` int DEFAULT NULL,
  `travel_duration_mins` int DEFAULT 0,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `trip_id` (`trip_id`),
  KEY `location_id` (`location_id`),
  KEY `transport_id` (`transport_id`),
  CONSTRAINT `trip_locations_ibfk_1` FOREIGN KEY (`trip_id`) REFERENCES `user_trips` (`id`) ON DELETE CASCADE,
  CONSTRAINT `trip_locations_ibfk_2` FOREIGN KEY (`location_id`) REFERENCES `locations` (`id`) ON DELETE CASCADE,
  CONSTRAINT `trip_locations_ibfk_3` FOREIGN KEY (`transport_id`) REFERENCES `transport_types` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 7. ตาราง subscription_plans (แพ็กเกจสมาชิก)
CREATE TABLE IF NOT EXISTS `subscription_plans` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `description` text,
  `price` decimal(8,2) NOT NULL,
  `duration_days` int NOT NULL,
  `is_active` tinyint(1) DEFAULT 1,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 8. ตาราง advertisements (ป้ายโฆษณา)
CREATE TABLE IF NOT EXISTS `advertisements` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `title` varchar(150) NOT NULL,
  `image_url` varchar(255) NOT NULL,
  `redirect_url` varchar(255) NOT NULL,
  `start_date` date NOT NULL,
  `end_date` date NOT NULL,
  `status` enum('active','paused','expired','rejected') DEFAULT 'active',
  `views_count` int DEFAULT 0,
  `clicks_count` int DEFAULT 0,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `advertisements_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 9. ตาราง payments (การชำระเงินค่าสมาชิกและโฆษณา)
CREATE TABLE IF NOT EXISTS `payments` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `subscription_plan_id` int DEFAULT NULL,
  `advertisement_id` int DEFAULT NULL,
  `amount` decimal(8,2) NOT NULL,
  `payment_method` enum('credit_card','promptpay','bank_transfer','other') NOT NULL,
  `payment_status` enum('pending','completed','failed','refunded') DEFAULT 'pending',
  `transaction_ref` varchar(100) DEFAULT NULL,
  `paid_at` datetime DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `transaction_ref` (`transaction_ref`),
  KEY `user_id` (`user_id`),
  KEY `subscription_plan_id` (`subscription_plan_id`),
  KEY `advertisement_id` (`advertisement_id`),
  CONSTRAINT `payments_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `payments_ibfk_2` FOREIGN KEY (`subscription_plan_id`) REFERENCES `subscription_plans` (`id`) ON DELETE SET NULL,
  CONSTRAINT `payments_ibfk_3` FOREIGN KEY (`advertisement_id`) REFERENCES `advertisements` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 10. ตาราง support_tickets (แจ้งปัญหาการใช้งาน)
CREATE TABLE IF NOT EXISTS `support_tickets` (
  `id` int NOT NULL AUTO_INCREMENT,
  `customer_id` int NOT NULL,
  `employee_id` int DEFAULT NULL,
  `subject` varchar(255) NOT NULL,
  `description` text NOT NULL,
  `image_url` varchar(255) DEFAULT NULL,
  `status` enum('open','in_progress','resolved','closed') DEFAULT 'open',
  `priority` enum('low','medium','high','urgent') DEFAULT 'medium',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `customer_id` (`customer_id`),
  KEY `employee_id` (`employee_id`),
  CONSTRAINT `support_tickets_ibfk_1` FOREIGN KEY (`customer_id`) REFERENCES `users` (`id`),
  CONSTRAINT `support_tickets_ibfk_2` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- เพิ่มข้อมูลตัวอย่าง (Seed Data) สำหรับพรีเซนต์ 50% และงาน Lab
-- ============================================================================

-- ข้อมูลตัวอย่างสำหรับตาราง labstudent (เผื่อใช้เปิดหน้า Home/Lab9)
INSERT INTO `labstudent` (`StdId`, `StdPassword`, `StdName`, `StdLastname`) VALUES
('66009922', '1234', 'สุทธิพงษ์', 'ทางลัด');

-- เพิ่มบัญชีผู้ใช้งานครบทุก Role และแผนก (รหัสผ่านทุกบัญชีคือ 1234)
INSERT INTO `users` (`id`, `userName`, `email`, `password`, `phone_number`, `role`, `status`, `last_active`) VALUES
(1, 'customer01', 'customer@tanglat.com', '1234', '0812345678', 'customer', 'active', NOW()),
(2, 'vip_user', 'vip@tanglat.com', '1234', '0898765432', 'customer', 'active', NOW()),
(3, 'support01', 'support@tanglat.com', '1234', '0821112222', 'employee', 'active', NOW()),
(4, 'dev01', 'dev@tanglat.com', '1234', '0833334444', 'employee', 'active', NOW()),
(5, 'marketing01', 'marketing@tanglat.com', '1234', '0845556666', 'employee', 'active', NOW()),
(6, 'accounting01', 'accounting@tanglat.com', '1234', '0857778888', 'employee', 'active', NOW()),
(7, 'admin01', 'admin@tanglat.com', '1234', '0809990000', 'admin', 'active', NOW());

-- เพิ่มข้อมูลพนักงาน 4 แผนก พร้อมเงินเดือน (Salary)
INSERT INTO `employees` (`id`, `user_id`, `first_name`, `last_name`, `position`, `department`, `salary`, `hire_date`) VALUES
(1, 3, 'พิมพ์ชนก', 'ใจดี', 'Customer Support Officer', 'Support', 26000.00, '2025-01-15'),
(2, 4, 'สุทธิพงษ์', 'พัฒนาระบบ', 'Full-Stack Developer', 'Dev', 45000.00, '2024-11-01'),
(3, 5, 'นภัสสร', 'สื่อสารการตลาด', 'Marketing Specialist', 'Marketing', 34000.00, '2025-02-10'),
(4, 6, 'ธนดล', 'การคลัง', 'Senior Accountant', 'Accounting', 38000.00, '2024-12-01');

-- เพิ่มข้อมูลสถานที่ท่องเที่ยวสำคัญในกรุงเทพฯ (พิกัดจริงสำหรับ Leaflet.js)
INSERT INTO `locations` (`id`, `name`, `description`, `address`, `latitude`, `longitude`, `category`, `image_url`, `is_active`) VALUES
(1, 'วัดพระศรีรัตนศาสดาราม (วัดพระแก้ว)', 'วัดคู่บ้านคู่เมืองในพระบรมมหาราชวัง ชมความงามของพระแก้วมรกตและสถาปัตยกรรมไทย', 'ถนนหน้าพระลาน แขวงพระบรมมหาราชวัง เขตพระนคร กรุงเทพฯ', 13.75164400, 100.49270400, 'attraction', 'https://images.unsplash.com/photo-1563492065599-3520f775eeed?auto=format&fit=crop&w=600&q=80', 1),
(2, 'วัดอรุณราชวราราม (วัดแจ้ง)', 'พระปรางค์วัดอรุณริมแม่น้ำเจ้าพระยา จุดชมวิวพระอาทิตย์ตกยอดนิยม', 'ถนนวังเดิม แขวงวัดอรุณ เขตบางกอกใหญ่ กรุงเทพฯ', 13.74388400, 100.48844600, 'attraction', 'https://images.unsplash.com/photo-1528181304800-259b08848526?auto=format&fit=crop&w=600&q=80', 1),
(3, 'เยาวราช (Chinatown Bangkok)', 'สวรรค์ของนักชิมสตรีทฟู้ดยามค่ำคืนและแหล่งวัฒนธรรมไทย-จีน', 'ถนนเยาวราช แขวงสัมพันธวงศ์ เขตสัมพันธวงศ์ กรุงเทพฯ', 13.74113600, 100.50830500, 'restaurant', 'https://images.unsplash.com/photo-1552465011-b4e21bf6e79a?auto=format&fit=crop&w=600&q=80', 1),
(4, 'สยามพารากอน (Siam Paragon)', 'ศูนย์การค้าใจกลางสยาม เชื่อมต่อสถานีรถไฟฟ้า BTS สยาม เดินทางสะดวก', '991 ถนนพระรามที่ 1 แขวงปทุมวัน เขตปทุมวัน กรุงเทพฯ', 13.74624100, 100.53474000, 'shopping', 'https://images.unsplash.com/photo-1519567241046-7f570eee3ce6?auto=format&fit=crop&w=600&q=80', 1),
(5, 'ไอคอนสยาม (ICONSIAM)', 'แลนด์มาร์คริมแม่น้ำเจ้าพระยา รวมแหล่งช้อปปิ้งและโซนสุขสยาม', '299 ถนนเจริญนคร แขวงคลองต้นไทร เขตคลองสาน กรุงเทพฯ', 13.72672000, 100.51052400, 'shopping', 'https://images.unsplash.com/photo-1583396618422-597b2755de83?auto=format&fit=crop&w=600&q=80', 1),
(6, 'สวนเบญจกิติ (Benchakitti Park)', 'สวนสาธารณะและสวนป่ากลางกรุง จุดเดินเล่นถ่ายรูปสกายวอล์ค', 'ถนนรัชดาภิเษก แขวงคลองเตย เขตคลองเตย กรุงเทพฯ', 13.72994200, 100.55824600, 'attraction', 'https://images.unsplash.com/photo-1508009603885-50cf7c579365?auto=format&fit=crop&w=600&q=80', 1),
(7, 'ตลาดนัดจตุจักร (Chatuchak Market)', 'แหล่งช้อปปิ้งสินค้าแฟชั่น ของแต่งบ้าน และต้นไม้ที่ใหญ่ที่สุดในไทย', 'ถนนกำแพงเพชร 2 แขวงจตุจักร เขตจตุจักร กรุงเทพฯ', 13.79992000, 100.55084200, 'shopping', 'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=600&q=80', 1),
(8, 'หอศิลปวัฒนธรรมแห่งกรุงเทพมหานคร (BACC)', 'พื้นที่จัดแสดงงานศิลปะร่วมสมัย นิทรรศการ และคาเฟ่แยกปทุมวัน', '939 ถนนพระรามที่ 1 แขวงวังใหม่ เขตปทุมวัน กรุงเทพฯ', 13.74667200, 100.53035000, 'attraction', 'https://images.unsplash.com/photo-1518998053901-5348d3961a04?auto=format&fit=crop&w=600&q=80', 1);

-- เพิ่มข้อมูลระบบขนส่งสาธารณะ 3 ประเภทหลัก
INSERT INTO `transport_types` (`id`, `name`, `type`, `base_fare`, `fare_per_km`, `fare_per_station`, `description`, `is_active`) VALUES
(1, 'รถเมล์ขสมก. (AC Bus)', 'bus', 15.00, 2.00, 0.00, 'รถโดยสารประจำทางปรับอากาศ ประหยัดคุ้มค่า ครอบคลุมทั่วกรุงเทพฯ', 1),
(2, 'รถไฟฟ้า BTS / MRT', 'train', 17.00, 0.00, 4.00, 'รถไฟฟ้าสายสีเขียวและสีน้ำเงิน รวดเร็ว ไม่ติดไฟแดง (เฉลี่ยสถานีละ 1.2 กม.)', 1),
(3, 'รถมอเตอร์ไซค์รับจ้าง (วิน)', 'motorcycle', 25.00, 10.00, 0.00, 'มอเตอร์ไซค์วินรับจ้าง คล่องตัว เข้าถึงซอยลัดและจุดเชื่อมต่อได้ทันที', 1);

-- เพิ่มข้อมูลแพ็กเกจสมาชิก (Subscription Plans)
INSERT INTO `subscription_plans` (`id`, `name`, `description`, `price`, `duration_days`, `is_active`) VALUES
(1, 'Tang-Lat VIP Monthly (รายเดือน)', 'ปลดล็อกโฆษณาทั้งหมด (No Ads), จัดรูทปักหมุดสถานที่ได้ไม่จำกัดต่อทริป และบันทึกทริปโปรดได้ไม่จำกัด นาน 30 วัน', 99.00, 30, 1),
(2, 'Tang-Lat VIP Yearly (รายปีสุดคุ้ม)', 'ปลดล็อกโฆษณาทั้งหมด (No Ads), จัดรูทไม่จำกัด พร้อมป้ายสถานะ VIP พิเศษ นาน 365 วัน (ประหยัดกว่า 30%)', 799.00, 365, 1),
(3, 'แพ็กเกจลงป้ายโฆษณา (Ad Banner 15 วัน)', 'โปรโมทร้านค้า คาเฟ่ หรือที่เที่ยวของคุณบนหน้าแผนที่หลักและหน้าจัดทริป นาน 15 วัน', 499.00, 15, 1);

-- เพิ่มข้อมูลป้ายโฆษณาตัวอย่าง
INSERT INTO `advertisements` (`id`, `user_id`, `title`, `image_url`, `redirect_url`, `start_date`, `end_date`, `status`, `views_count`, `clicks_count`) VALUES
(1, 2, 'Chao Phraya River Cafe - ลด 20% สำหรับผู้ใช้แอปทางลัด', 'https://images.unsplash.com/photo-1554118811-1e0d58224f24?auto=format&fit=crop&w=800&q=80', 'https://example.com/cafe-promo', '2026-11-01', '2026-12-31', 'active', 1240, 185),
(2, 1, 'ทัวร์เรือด่วนเจ้าพระยา ชมวิววัดอรุณยามเย็น ราคาพิเศษ', 'https://images.unsplash.com/photo-1506665531195-3566af2b4dfa?auto=format&fit=crop&w=800&q=80', 'https://example.com/boat-tour', '2026-11-05', '2026-11-30', 'active', 890, 92),
(3, 1, 'Siam Street Food Festival 2026', 'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80', 'https://example.com/food-fest', '2026-11-10', '2026-11-25', 'paused', 310, 24);

-- เพิ่มข้อมูลการชำระเงินตัวอย่าง
INSERT INTO `payments` (`id`, `user_id`, `subscription_plan_id`, `advertisement_id`, `amount`, `payment_method`, `payment_status`, `transaction_ref`, `paid_at`) VALUES
(1, 2, 1, NULL, 99.00, 'promptpay', 'completed', 'TXN-20261101-001', '2026-11-01 10:30:00'),
(2, 2, 3, 1, 499.00, 'promptpay', 'completed', 'TXN-20261101-002', '2026-11-01 14:15:00'),
(3, 1, 1, NULL, 99.00, 'promptpay', 'pending', 'TXN-20261110-003', NOW()),
(4, 1, 3, 3, 499.00, 'bank_transfer', 'pending', 'TXN-20261110-004', NOW());

-- เพิ่มข้อมูลทริปตัวอย่างของผู้ใช้
INSERT INTO `user_trips` (`id`, `user_id`, `trip_name`, `total_distance_km`, `estimated_cost`, `is_favorite`) VALUES
(1, 1, 'ทริปไหว้พระ-ชิมของอร่อยรอบเกาะรัตนโกสินทร์', 6.45, 58.00, 1);

INSERT INTO `trip_locations` (`id`, `trip_id`, `location_id`, `stop_order`, `transport_id`, `travel_duration_mins`) VALUES
(1, 1, 1, 1, 1, 0),
(2, 1, 2, 2, 3, 12),
(3, 1, 3, 3, 2, 15),
(4, 1, 4, 4, 2, 10);

-- เพิ่มข้อมูลใบแจ้งปัญหา (Support Tickets) ตัวอย่าง
INSERT INTO `support_tickets` (`id`, `customer_id`, `employee_id`, `subject`, `description`, `image_url`, `status`, `priority`) VALUES
(1, 1, 2, '[ส่งต่อแผนก Dev] พิกัดหมุดวัดอรุณคลาดเคลื่อนเล็กน้อย', 'เวลาซูมแผนที่เข้าไปใกล้ๆ หมุดวัดอรุณอยู่ขยับไปทางท่าเรือ รบกวนทีม Dev ช่วยปรับพิกัดให้ตรงพระปรางค์ด้วยครับ', NULL, 'in_progress', 'high'),
(2, 2, 4, '[ส่งต่อแผนก Accounting] โอนเงินค่าโฆษณาแล้วแต่สถานะยังขึ้นรออนุมัติ', 'โอนเงินผ่านพร้อมเพย์ยอด 499 บาท รหัสอ้างอิง TXN-20261110-004 รบกวนฝ่ายบัญชีตรวจสอบยอดให้ด้วยครับ', NULL, 'open', 'urgent'),
(3, 1, 1, 'สอบถามวิธีเพิ่มสถานที่มากกว่า 3 แห่งใน 1 ทริป', 'ต้องการจัดรูทไปเที่ยว 5 ที่ในวันเดียว ต้องสมัครแพ็กเกจไหนครับ?', NULL, 'resolved', 'low');

SET FOREIGN_KEY_CHECKS = 1;