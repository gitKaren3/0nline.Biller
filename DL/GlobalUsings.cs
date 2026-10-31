// The database has a table named "File", so DBModelGenerator emits an entity
// called File, while the SDK's implicit usings bring System.IO.File into scope.
// The generated FileSqlQueryProvider refers to the entity by simple name, so
// the alias is what makes that file compile. Keeping it here (rather than in
// the generated file) survives regeneration.
global using File = _0nline.Biller.DL.Contract.Models.db.File;
