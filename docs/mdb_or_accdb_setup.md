# mdb or accdb setup  
  
In order to make the provider work, the table  
```MSysRelationships```  
should be accessible from Administrator  
  
In order to work EF Provider need a DUAL table (see [https://en.wikipedia.org/wiki/DUAL_table](https://en.wikipedia.org/wiki/DUAL_table)) 

**mdb files***  
By default, the table ```MSysRelationships``` must be accessible from Administrator in order to make the provider work.  
After the ```MSysRelationships``` table is made accessible the configuration must be done setting the right dual table (very early in your code)     
```c#
JetConnection.DUAL = JetConnection.DUALForMdb;
```
If it's not possible to make the ```MSysRelationships``` table readable, it is possible to use another table (see below).  

**accdb files***  
This is the default configuration so no needs to make changes.  
Access since version 2003 has visible system tables so no need to assign rights.  
The configuration must be done setting the right dual table (very early in your code).
```c#
JetConnection.DUAL = JetConnection.DUALForAccdb;
```

**Use a custom table**  
You can also setup a different DUAL table using JetConnection.DUAL static property.  
You can use a generic table and make a count on it or you can create a DUAL table (single record table) on access database and just set the name of the table in JetConnection.DUAL property i.e.  
  
```sql
CREATE TABLE Dual (id COUNTER CONSTRAINT pkey PRIMARY KEY)
INSERT INTO Dual (id) VALUES (1)
ALTER TABLE Dual ADD CONSTRAINT DualTableConstraint CHECK ((SELECT Count(*) FROM Dual) = 1)
```

and then, very early in your code:

```c#
JetConnection.DUAL = "Dual";
```
