INSERT INTO Brands (Name) VALUES 
('Nike'),
('Adidas'),
('Puma'),
('Jordan'),
('Reebok');

INSERT INTO Categories (Name) VALUES 
('Giày thể thao'),
('Áo thun'),
('Quần thể thao'),
('Phụ kiện'),
('Balo');

INSERT INTO Products (Name, Price, BrandId, Description, CategoryId) VALUES 
('Giày Nike Air Force 1', 2500000, 1, 'Giày sneaker cổ điển của Nike', 1),
('Áo thun Adidas Originals', 800000, 2, 'Áo thun unisex phong cách', 2),
('Quần jogger Puma', 1200000, 3, 'Quần thể thao thoải mái', 3),
('Nón Jordan Classic', 500000, 4, 'Nón thể thao phong cách Jordan', 4),
('Balo Reebok Active', 1500000, 5, 'Balo tiện lợi cho người năng động', 5);
INSERT INTO ProductImages (ProductId, ImageUrl) VALUES 
(1, 'img/logo1.png'),
(2, 'img/logo2.png'),
(3, 'img/logo3.png'),
(4, 'img/logo4.jpg'),
(5, 'img/logo5.jpg'),
(1, 'img/logo6.png'),
(2, 'img/logo7.png'),
(3, 'img/logo8.png');
INSERT INTO Sizes (size) VALUES 
(40),
(41),
(42),
(43),
(44);
INSERT INTO ProductSizes (ProductId, SizeId, Stock) VALUES 
(1, 1, 10),
(1, 2, 8),
(1, 2, 15),
(3, 3, 5),
(3, 3, 12);
