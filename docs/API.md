# Hotel Booking System API Reference

This document describes the available HTTP endpoints, request data,
authentication rules, successful responses, and important error responses.

## Base URL and interactive documentation

The default development base URLs are:

- `https://localhost:7288`
- `http://localhost:5270`

Swagger UI is available in the `Development` environment at:

```text
https://localhost:7288/swagger
```

JSON request and response property names are shown in `camelCase`. Dates must be
valid ISO 8601 values, for example `2030-06-10T00:00:00Z`. Enum values are
serialized as names rather than numbers.

Supported enum values:

- `HotelType`: `Luxury`, `Budget`, `Boutique`
- `RoomType`: `Single`, `Double`, `Twin`, `Suite`
- `BookingStatus`: `Pending`, `Confirmed`, `Cancelled`, `Completed`
- `PaymentStatus`: `Pending`, `Paid`, `Failed`

## Authentication and authorization

Registration and login are public. Every other endpoint requires a JWT with the
role shown in this document.

Send an access token using this header:

```http
Authorization: Bearer <token>
```

In Swagger, call the login endpoint, copy the returned `token`, select
**Authorize**, and paste the token. Swagger supplies the `Bearer` scheme.

Roles:

- **Customer**: hotel discovery, booking, cart, checkout, reviews, invoices, and
  recommendations.
- **Admin**: management of cities, hotels, rooms, promotions, and nearby
  attractions.

Authentication failures:

- `401 Unauthorized`: token is missing, invalid, expired, or lacks a valid user
  identifier where one is required.
- `403 Forbidden`: the token is valid but its role is not allowed.

## Response and error conventions

Successful endpoints return JSON unless the response is `204 No Content` or a
PDF download. Important status codes are:

| Status | Meaning |
| --- | --- |
| `200 OK` | Request succeeded. |
| `201 Created` | Resource was created. |
| `204 No Content` | Action succeeded without a response body. |
| `400 Bad Request` | Request validation or a business rule failed. |
| `401 Unauthorized` | Authentication failed. |
| `403 Forbidden` | The authenticated role is not allowed. |
| `404 Not Found` | The requested entity was not found or is not owned by the customer. |
| `409 Conflict` | Current state prevents the operation. |

Application-generated errors use this shape:

```json
{
  "message": "Explanation of the error"
}
```

Automatic ASP.NET Core model-validation errors return a validation problem
response containing an `errors` object keyed by field name.

## Endpoint summary

| Method | Route | Role | Success |
| --- | --- | --- | --- |
| `POST` | `/api/Auth/register` | Public | `200` |
| `POST` | `/api/Auth/login` | Public | `200` |
| `GET` | `/api/hotels/{hotelId}/available-rooms` | Customer | `200` |
| `POST` | `/api/hotels/{hotelId}/available-rooms/{roomId}/selection` | Customer | `200` |
| `PUT` | `/api/bookings/{bookingId}` | Customer | `204` |
| `DELETE` | `/api/bookings/{bookingId}` | Customer | `204` |
| `POST` | `/api/cart/items` | Customer | `204` |
| `GET` | `/api/cart` | Customer | `200` |
| `DELETE` | `/api/cart/items/{cartItemId}` | Customer | `204` |
| `POST` | `/api/checkout` | Customer | `200` |
| `GET` | `/api/Cities` | Admin | `200` |
| `POST` | `/api/Cities` | Admin | `201` |
| `PUT` | `/api/Cities/{cityId}` | Admin | `200` |
| `DELETE` | `/api/Cities/{cityId}` | Admin | `204` |
| `GET` | `/api/FeaturedDeals` | Customer | `200` |
| `GET` | `/api/hotels/{hotelId}` | Customer | `200` |
| `GET` | `/api/hotels/{hotelId}/images` | Customer | `200` |
| `GET` | `/api/hotels/{hotelId}/location` | Customer | `200` |
| `GET` | `/api/hotels/{hotelId}/reviews` | Customer | `200` |
| `POST` | `/api/hotels/{hotelId}/reviews` | Customer | `204` |
| `GET` | `/api/Hotels` | Admin | `200` |
| `POST` | `/api/Hotels` | Admin | `201` |
| `PUT` | `/api/Hotels/{hotelId}` | Admin | `200` |
| `PATCH` | `/api/Hotels/{hotelId}/status` | Admin | `204` |
| `GET` | `/api/invoices/{invoiceId}/pdf` | Customer | `200` PDF |
| `POST` | `/api/nearby-attractions` | Admin | `201` |
| `GET` | `/api/nearby-attractions/hotel/{hotelId}` | Admin | `200` |
| `PUT` | `/api/nearby-attractions/{attractionId}` | Admin | `204` |
| `DELETE` | `/api/nearby-attractions/{attractionId}` | Admin | `204` |
| `POST` | `/api/promotions` | Admin | `201` |
| `PATCH` | `/api/promotions/{promotionId}/status?isActive={bool}` | Admin | `204` |
| `GET` | `/api/hotels/recently-visited` | Customer | `200` |
| `GET` | `/api/Rooms` | Admin | `200` |
| `POST` | `/api/Rooms` | Admin | `201` |
| `PUT` | `/api/Rooms/{roomId}` | Admin | `200` |
| `PATCH` | `/api/Rooms/{roomId}/status` | Admin | `204` |
| `PATCH` | `/api/Rooms/{roomId}/operational-availability` | Admin | `204` |
| `POST` | `/api/Rooms/{roomId}/images` | Admin | `204` |
| `DELETE` | `/api/Rooms/{roomId}/images/{imageId}` | Admin | `204` |
| `GET` | `/api/Search` | Customer | `200` |
| `GET` | `/api/hotels/trending-destinations` | Customer | `200` |

## Authentication

### Register a customer

`POST /api/Auth/register` — Public

Request body:

```json
{
  "firstName": "Alaa",
  "lastName": "Example",
  "username": "<customer-username>",
  "email": "alaa@example.com",
  "password": "<customer-password>"
}
```

All fields are required. Names and username must contain 2–100 characters,
email must be valid and at most 100 characters, and password must contain at
least 8 characters.

Success: `200 OK` with no body.

Important errors: `400` for invalid input; `409` when the username or email is
already registered.

### Login

`POST /api/Auth/login` — Public

Request body:

```json
{
  "username": "<customer-username>",
  "password": "<customer-password>"
}
```

Success: `200 OK`:

```json
{
  "token": "eyJ..."
}
```

Important errors: `400` for invalid field lengths; `401` for invalid
credentials.

## Hotel discovery

### Search hotels

`GET /api/Search` — Customer

Required query parameters:

| Parameter | Rule |
| --- | --- |
| `destination` | Non-empty hotel or city search text. |
| `checkIn` | ISO 8601 date/time. |
| `checkOut` | Must be after `checkIn`. |
| `adults` | At least `1`. |
| `children` | `0` or greater. |
| `rooms` | At least `1`. |

Optional query parameters: `minPrice` and `maxPrice` (non-negative and minimum
cannot exceed maximum), `minRating` (`1`–`5`), repeated `amenityIds` values
(positive IDs), `hotelType`, `roomType`, and `pageNumber` (defaults to `1`, page
size is 10).

Example:

```http
GET /api/Search?destination=Bethlehem&checkIn=2030-06-10T00:00:00Z&checkOut=2030-06-13T00:00:00Z&adults=2&children=1&rooms=1&minRating=4&hotelType=Luxury&pageNumber=1
```

Success: `200 OK`:

```json
{
  "items": [
    {
      "hotelId": 12,
      "name": "Example Hotel",
      "city": "Bethlehem",
      "address": "Main Street",
      "hotelType": "Luxury",
      "startingPrice": 120.00,
      "rating": 4.5,
      "thumbnailUrl": "https://example.com/hotel.jpg",
      "briefDescription": "City-centre hotel"
    }
  ],
  "pageNumber": 1,
  "hasNextPage": false
}
```

Important errors: `400` for any invalid search rule listed above.

### Get hotel details

`GET /api/hotels/{hotelId}` — Customer

Success: `200 OK` with `hotelId`, `name`, `city`, `description`, `history`,
`hotelType`, `address`, `latitude`, `longitude`, nullable `rating`, and an
`amenities` array containing `name` and nullable `description`. Accessing this
endpoint also records the hotel in the customer's recently visited list.

Important errors: `404` when the hotel does not exist.

### Get hotel images

`GET /api/hotels/{hotelId}/images` — Customer

Success: `200 OK` with an array of `{ "imageUrl": "...", "displayOrder": 1 }`
objects ordered for display.

Important errors: `404` when the hotel does not exist.

### Get hotel location

`GET /api/hotels/{hotelId}/location` — Customer

Success: `200 OK` with `latitude`, `longitude`, and an `attractions` array. Each
attraction contains `name`, nullable `description`, `latitude`, and `longitude`.

Important errors: `404` when the hotel does not exist.

### Get hotel reviews

`GET /api/hotels/{hotelId}/reviews` — Customer

Success: `200 OK` with nullable average `rating` and a `reviews` array. Each
review contains `rating`, `comment`, and `createdAt`.

Important errors: `404` when the hotel does not exist.

### Submit a hotel review

`POST /api/hotels/{hotelId}/reviews` — Customer

Request body:

```json
{
  "bookingId": 34,
  "rating": 5,
  "comment": "Excellent stay"
}
```

Rating must be `1`–`5`, and comment cannot be empty. The booking must belong to
the authenticated customer and specified hotel, be completed, have an ended
stay, and not already have a review.

Success: `204 No Content`.

Important errors: `400` for an invalid rating/comment or an ineligible booking;
`404` when the booking does not exist.

### Get available rooms

`GET /api/hotels/{hotelId}/available-rooms` — Customer

Query parameters: `checkIn`, `checkOut`, `adults`, and `children`. Dates are
required, checkout must be later than check-in, adults must be positive, and
children cannot be negative.

Success: `200 OK` with an array containing `roomId`, `roomType`, nullable
`description`, capacities, `pricePerNight`, and ordered room `images`.

Important errors: `400` for invalid dates or guest counts; `404` when the hotel
does not exist.

### Price/select an available room

`POST /api/hotels/{hotelId}/available-rooms/{roomId}/selection` — Customer

Request body:

```json
{
  "checkIn": "2030-06-10T00:00:00Z",
  "checkOut": "2030-06-13T00:00:00Z",
  "adults": 2,
  "children": 1
}
```

Success: `200 OK` with `roomId`, `roomType`, requested dates and guest counts,
`pricePerNight`, and calculated `totalPrice`.

Important errors: `400` for invalid request data or when the room is not
available for the requested stay.

### Get featured deals

`GET /api/FeaturedDeals` — Customer

Success: `200 OK` with up to five deal objects containing `hotelId`,
`hotelName`, `city`, `address`, nullable `thumbnailUrl`, nullable `rating`,
`originalPrice`, `discountPercentage`, and `discountedPrice`.

### Get recently visited hotels

`GET /api/hotels/recently-visited` — Customer

Success: `200 OK` with up to five newest active hotels visited by the customer.
Each item contains `hotelId`, `name`, `city`, nullable `rating`, and nullable
`startingPricePerNight`.

### Get trending destinations

`GET /api/hotels/trending-destinations` — Customer

Success: `200 OK` with up to five cities ordered by valid booking count during
the last 30 days. Each item contains `cityId`, `name`, `country`, and
`bookingCount`.

## Cart, checkout, and bookings

### Add an item to the cart

`POST /api/cart/items` — Customer

Request body:

```json
{
  "roomId": 20,
  "checkIn": "2030-06-10T00:00:00Z",
  "checkOut": "2030-06-13T00:00:00Z",
  "adults": 2,
  "children": 1
}
```

Check-in cannot be in the past, checkout must be later, at least one adult is
required, and children cannot be negative.

Success: `204 No Content`.

Important errors: `400` for invalid dates or guest counts; `409` when the room
is unavailable for the dates or capacity.

### Get the cart

`GET /api/cart` — Customer

Success: `200 OK` with an array of cart items containing `cartItemId`, `roomId`,
`roomNumber`, `hotelId`, `hotelName`, `checkIn`, `checkOut`, `adults`, and
`children`.

### Remove a cart item

`DELETE /api/cart/items/{cartItemId}` — Customer

Success: `204 No Content`.

Important errors: `404` when the cart item does not exist or does not belong to
the authenticated customer.

### Complete checkout

`POST /api/checkout` — Customer

Request body:

```json
{
  "specialRequests": "Late arrival",
  "payment": {
    "shouldSucceed": true
  }
}
```

`payment` is required. `specialRequests` is optional and limited to 1000
characters. `shouldSucceed` is the current payment simulation input.

Success: `200 OK` with:

- `bookingCount` and `invoiceCount`.
- `payments`: items containing `amount` and `status`.
- `confirmations`: hotel-level items containing `confirmationId`, `hotelId`,
  `hotelName`, `totalAmount`, `paymentStatus`, and booked `rooms`.
- Each confirmation room contains `bookingId`, `roomId`, `roomNumber`,
  `checkIn`, `checkOut`, and `totalAmount`.

Important errors: `400` when payment data is absent, the cart is empty, or
special requests are too long; `404` when customer email is unavailable; `409`
when a cart room is no longer available.

### Modify a booking

`PUT /api/bookings/{bookingId}` — Customer

Request body:

```json
{
  "roomId": 21,
  "checkIn": "2030-07-01T00:00:00Z",
  "checkOut": "2030-07-04T00:00:00Z",
  "adults": 2,
  "children": 0,
  "specialRequests": "Quiet room"
}
```

The room ID must be positive, checkout must follow check-in, at least one adult
is required, children cannot be negative, and special requests are limited to
1000 characters. A replacement room must belong to the same hotel and fit the
guest counts.

Success: `204 No Content`.

Important errors: `400` for invalid request/capacity; `404` when the booking or
room is unavailable to the customer; `409` when the stay has started, booking is
cancelled, or the requested room/dates are unavailable.

### Cancel a booking

`DELETE /api/bookings/{bookingId}` — Customer

Success: `204 No Content`.

Important errors: `404` when the booking does not exist or is not owned by the
customer; `409` when it is already cancelled or its stay has started.

### Download an invoice

`GET /api/invoices/{invoiceId}/pdf` — Customer

Success: `200 OK`, content type `application/pdf`, downloaded as
`invoice-{invoiceId}.pdf`.

Important errors: `400` for a non-positive invoice ID; `404` when the invoice
does not exist or does not belong to the authenticated customer.

## City administration

All city endpoints require the **Admin** role.

### List cities

`GET /api/Cities?search={text}`

`search` is optional and matches city data. Success: `200 OK` with an array of
`cityId`, `name`, `country`, and `postOffice`.

### Create a city

`POST /api/Cities`

```json
{
  "name": "Bethlehem",
  "country": "Palestine",
  "postOffice": "P100"
}
```

All fields are required; name and country are limited to 50 characters.

Success: `201 Created` with the created city response.

Important errors: `400` for invalid fields.

### Update a city

`PUT /api/Cities/{cityId}`

Uses the same body as create. Success: `200 OK` with the updated city.

Important errors: `400` for invalid fields; `404` when the city does not exist.

### Delete a city

`DELETE /api/Cities/{cityId}`

Success: `204 No Content`.

Important errors: `404` when the city does not exist; `409` when it has
associated hotels.

## Hotel administration

All hotel administration endpoints require the **Admin** role.

### List hotels

`GET /api/Hotels?search={text}`

`search` is optional and matches hotel name or owner. Success: `200 OK` with an
array containing `hotelId`, `cityId`, `name`, `ownerName`, `address`, coordinates,
`hotelType`, nullable `description` and `history`, and `isActive`.

### Create a hotel

`POST /api/Hotels`

```json
{
  "cityId": 1,
  "name": "Example Hotel",
  "ownerName": "Example Owner",
  "address": "Main Street",
  "latitude": 31.7054,
  "longitude": 35.2024,
  "hotelType": "Luxury",
  "description": "Central hotel",
  "history": null
}
```

`cityId` must be positive; name, owner, and address are required; latitude must
be `-90`–`90`; longitude must be `-180`–`180`; and hotel type must be valid.

Success: `201 Created` with the created hotel response.

Important errors: `400` for invalid fields; `404` when the city does not exist.

### Update a hotel

`PUT /api/Hotels/{hotelId}`

Uses the same body as create. Success: `200 OK` with the updated hotel.

Important errors: `400` for invalid fields; `404` when the hotel or requested
city does not exist.

### Change hotel status

`PATCH /api/Hotels/{hotelId}/status`

```json
{
  "isActive": false
}
```

Success: `204 No Content`. Important errors: `404` when the hotel does not exist.

## Room administration

All room endpoints require the **Admin** role.

### List/filter rooms

`GET /api/Rooms`

All query parameters are optional: `search` (room number), `hotelId`, `roomType`,
`isActive`, and `isOperationallyAvailable`.

Success: `200 OK` with an array containing `roomId`, `hotelId`, `roomNumber`,
`roomType`, capacities, `pricePerNight`, operational/active flags, nullable
`description`, and ordered `images`.

### Create a room

`POST /api/Rooms`

```json
{
  "hotelId": 1,
  "roomNumber": "205",
  "roomType": "Double",
  "adultsCapacity": 2,
  "childCapacity": 1,
  "pricePerNight": 125.00,
  "description": "City view"
}
```

Hotel ID and adult capacity must be positive, room number is required and at
most 50 characters, child capacity cannot be negative, price must be positive,
and room type must be valid.

Success: `201 Created` with the created room response.

Important errors: `400` for invalid fields; `404` when the hotel does not exist.

### Update a room

`PUT /api/Rooms/{roomId}`

Uses the same body as create. Success: `200 OK` with the updated room.

Important errors: `400` for invalid fields; `404` when the room or requested
hotel does not exist.

### Change room status

`PATCH /api/Rooms/{roomId}/status`

Body: `{ "isActive": false }`. Success: `204 No Content`.

Important errors: `404` when the room does not exist.

### Change operational availability

`PATCH /api/Rooms/{roomId}/operational-availability`

Body: `{ "isOperationallyAvailable": false }`. Success: `204 No Content`.

Important errors: `404` when the room does not exist.

### Add a room image

`POST /api/Rooms/{roomId}/images`

```json
{
  "imageUrl": "https://example.com/room.jpg",
  "displayOrder": 1
}
```

Image URL is required and limited to 500 characters; display order must be
positive. Success: `204 No Content`.

Important errors: `400` for invalid fields; `404` when the room does not exist.

### Delete a room image

`DELETE /api/Rooms/{roomId}/images/{imageId}`

Success: `204 No Content`.

Important errors: `404` when the room/image does not exist or the image does not
belong to the specified room.

## Promotion administration

All promotion endpoints require the **Admin** role.

### Create a promotion

`POST /api/promotions`

```json
{
  "hotelId": 1,
  "discountPercentage": 20,
  "startDate": "2030-06-01T00:00:00Z",
  "endDate": "2030-06-30T23:59:59Z"
}
```

Hotel ID must be positive, discount must be `1`–`99`, and end date must be after
start date. Success: `201 Created` with no body.

Important errors: `400` for invalid data; `404` when the hotel does not exist.

### Change promotion status

`PATCH /api/promotions/{promotionId}/status?isActive=true`

The nullable Boolean query parameter `isActive` is required. Success:
`204 No Content`.

Important errors: `400` when the promotion ID is invalid or `isActive` is
missing/invalid; `404` when the promotion does not exist.

## Nearby-attraction administration

All nearby-attraction endpoints require the **Admin** role.

### Create a nearby attraction

`POST /api/nearby-attractions`

```json
{
  "hotelId": 1,
  "name": "Historic Site",
  "description": "A short walk from the hotel",
  "latitude": 31.704,
  "longitude": 35.203
}
```

Hotel ID must be positive; name is required and limited to 100 characters;
description is optional and limited to 500 characters; latitude must be
`-90`–`90`; longitude must be `-180`–`180`.

Success: `201 Created` with no body.

Important errors: `400` for invalid data; `404` when the hotel does not exist.

### List attractions for a hotel

`GET /api/nearby-attractions/hotel/{hotelId}`

Success: `200 OK` with an array containing `nearbyAttractionId`, `hotelId`,
`name`, nullable `description`, `latitude`, and `longitude`.

Important errors: `400` for a non-positive hotel ID; `404` when the hotel does
not exist.

### Update an attraction

`PUT /api/nearby-attractions/{attractionId}`

```json
{
  "name": "Updated Site",
  "description": "Updated description",
  "latitude": 31.704,
  "longitude": 35.203
}
```

Uses the same text and coordinate validation as create. Success:
`204 No Content`.

Important errors: `400` for invalid ID/data; `404` when the attraction does not
exist.

### Delete an attraction

`DELETE /api/nearby-attractions/{attractionId}`

Success: `204 No Content`.

Important errors: `400` for a non-positive ID; `404` when the attraction does
not exist.

## Testing an endpoint with PowerShell

Login and save the returned token:

```powershell
$login = Invoke-RestMethod `
  -Method Post `
  -Uri "https://localhost:7288/api/Auth/login" `
  -ContentType "application/json" `
  -Body '{"username":"<customer-username>","password":"<customer-password>"}'

$headers = @{ Authorization = "Bearer $($login.token)" }
```

Call an authenticated endpoint:

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri "https://localhost:7288/api/hotels/trending-destinations" `
  -Headers $headers
```

Replace the credentials and IDs with values from the configured local database.
