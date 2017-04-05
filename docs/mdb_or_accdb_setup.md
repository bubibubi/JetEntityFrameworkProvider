# mdb or accdb setup  
  
In order to make the provider work, the table  
MSysRelationships  
should be accessible from Administrator  
  
**Version 1.1.2 update**  
In order to work EF Provider need a DUAL table (see [https://en.wikipedia.org/wiki/DUAL_table](https://en.wikipedia.org/wiki/DUAL_table))  
In previous versions the DUAL table was obtained using (SELECT COUNT(*) FROM MSysRelationships) query.  
Since version 1.1.2 you can also setup a different DUAL table using JetConnection.DUAL static property.  
You can use a generic table (Access since version 2003 has visible system tables) and make a count on it or you can create a DUAL table (single record table) on access database and just set the name of the table in JetConnection.DUAL property i.e.  
  
```sql
CREATE TABLE Dual (id COUNTER CONSTRAINT pkey PRIMARY KEY)
INSERT INTO Dual (id) VALUES (1)
ALTER TABLE Dual ADD CONSTRAINT DualTableConstraint CHECK ((SELECT Count(*) FROM Dual) = 1)
```

and then, very early in your code:

```c#
JetConnection.DUAL = "Dual";
```
