namespace BOEmbeddingService;

public static class Constants
{
    public static string KineticBusinessObjectImplementationDetails = """
	Implementation of some methods is split into separate stages in addition to base implementation. The base implementation of some key methods is as follows:
	* GetRows - returns full dataset with parent and child tables based on supplied conditions (known as "where clauses") that are defined in SQL-like syntax
	* GetList - return reduced single table dataset with small subset of fields for the parent table only. This again takes SQL-like where clause and is used for fast searches
	* GetByID - return full dataset for parent record specified by its primary key (one or more fields) and all the corresponding child records
	* GetBySysRowID - return full dataset for parent record specified by the supplied GUID identity key (not primary key!) and all corresponding child records
	* Update - apply changes in supplied dataset (full dataset from GetByID, GetBySysRowID or GetRows) and write them back to the database. This includes updates, creation and deletion.
	
	Base implementation of these methods can be augmented by implementing corresponding Before and After methods. For exmple, BeforeUpdate method runs before base update method.
	BeforeGetRows and AfterGetRows methods are special in that they run for any attempt o retrieve full dataset, so for GetRows, GetByID and GetBySysRowID.
	
	Update has special table specific overrides. For exmaple, if we have dataset with tables Parent and Child, implementation of base update functinality can be extended by
	implementing the following table-specific methods: ParentBeforeUpdate, ParentAfterUpdate, ParentBeforeCreate, ChildAfterDelete, etc. So Update() table specific implemenations have the following:
	* <Table>BeforeCreate - runs before new record is inserted into <Table> by Update
	* <Table>AfterCreate - runs after new record is inserted into <Table> by Update
	* <Table>BeforeUpdate - runs before existing record is updated in <Table> by Update
	* <Table>AfterUpdate - runs after existing record is updated in <Table> by Update
	* <Table>BeforeDelete - runs before existing record is deleted in <Table> by Update
	* <Table>AfterDelete - runs after existing record is updated in <Table> by Update
	
	These implementation extensions allow changing and adjusting how a given service implementation functions.
	Full dataset retrieval methods (GetRows, GetByID, GetBySysRowID) have special functions with postfix "ForeignLink". These are automatically invoked to retrieve the data from associated extra tables.
	For example, OrderHed record might include foreign link to Customer.Name as CustomerName which automatically looks up customer for the current record and populated value of the CustomerName column with Cutomer.Name. This will go into dataset table field/column which is not physically present in the database.
	
	Methods with suffic "GetNew" (e.g. TableGetNew) are used to add a new record to the dataset without committing it to database. They would usually populate defaults.
	Methods with prefix "OnChange" are used to validate changes in a specific field and often are used to calculate related values in dataset. They do not write these changes to database.
	
	Epicor Kinetic ERP uses the following terminology and abbreviations throughout it table and service names:
	* PO - purchase order
	* SO - sales order, often just referred to as order
	* AP - accounts payable
	* AR - accounts receivable
	* GL - general ledger
	* Tran - transaction
	* Hed and Head - header record
	* Dtl, Detail and Line - line record (child of header)
	* Rel and Release - release (child or line)
	""";
}
