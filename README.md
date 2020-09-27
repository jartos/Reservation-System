# Reservation System

In this app user can make cabin reservations. 
Registered user can become a cabin owner and add cabins to the reservation system.
Admin role is for managing all user, cabin, resort, reservation and activity information.

A registered user can make a cabin reservation and add activities to reservation. All the reservation information can be seen by user and
reservation can be cancelled one day before the reservation starts. The bill is sent to user as a PDF-file. 
User can make a request to the system admin and receive a cabin owner role. 
Cabin owner can add own cabins to the system and define the cabin information. 
Cabin owner can also add pictures of the cabin to the database and fully manage the reservations of the cabin. 
Admin role is for managing the all information in the reservation system with no limits. Admin can create and add newresorts to the system.
Admin can also see detailed reporting about reservations and activities by resort with graphicalpresentation. 
Reservation system uses ASP.NET Core Identity for managing logging users in and dividing user roles.


GitHub files include CabinReservationAPI (Backend) and CabinReservationWebApplication (Frontend).
App uses Microsoft Azure SQL Database for storing all information.
Application has been developed in Microsoft ASP.NET Core Framework with Visual Studio and
has been implemented with MVC (Model-View-Controller) design pattern.

Database structure:
<br>
<img src="https://hjtpictures.blob.core.windows.net/hjtpictures/Database.PNG" width="75%">
<br>
Picture made with MySQL Workbench by Jarno Tossavainen


Developers:
Juha Korhonen
Jarno Tossavainen
Pasi Yli-Pirilä
