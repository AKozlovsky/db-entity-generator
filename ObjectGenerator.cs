using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Web.Http.Hosting;

namespace DXApplication2
{
    class ObjectGenerator
    {
        /// <summary>
        /// Define the compile unit to use for code generation. 
        /// </summary>
        CodeCompileUnit compileUnit;

        /// <summary>
        /// The only class in the compile unit. This class contains 2 fields,
        /// 3 properties, a constructor, an entry point, and 1 simple method. 
        /// </summary>
        CodeTypeDeclaration codeClass;

        private string tableName;
        private string className;
        private DataTable columnsOfTable;

        public ObjectGenerator(string tableName, DB db)
        {
            this.tableName = tableName;
            columnsOfTable = db.GetColumnsOfTable(tableName);

            if (tableName.Contains('_'))
            {
                string partName1 = tableName.Split('_')[0].First().ToString().ToUpper() + tableName.Split('_')[0].Substring(1);
                string partName2 = tableName.Split('_')[1].First().ToString().ToUpper() + tableName.Split('_')[1].Substring(1);
                this.className = partName1 + partName2;
            }
            else
                this.className = tableName.First().ToString().ToUpper() + tableName.Substring(1);

            compileUnit = new CodeCompileUnit();

            AddUsingClasses();
            AddClass();
            AddMethods();
            AddProperties(db.GetColumnsOfTable(tableName));
        }

        public string ClassName
        {
            get
            {
                return this.className;
            }
        }

        /// <summary>
        /// Add a global using classes to the class.
        /// </summary>
        private void AddUsingClasses()
        {
            CodeNamespace globalNamespace = new CodeNamespace();
            globalNamespace.Imports.Add(new CodeNamespaceImport("System"));
            globalNamespace.Imports.Add(new CodeNamespaceImport("System.ComponentModel.DataAnnotations"));
            globalNamespace.Imports.Add(new CodeNamespaceImport("System.ComponentModel.DataAnnotations.Schema"));
            compileUnit.Namespaces.Add(globalNamespace);
        }

        /// <summary>
        /// Add a class.
        /// </summary>
        /// 
        private void AddClass()
        {
            CodeNamespace nameSpace = new CodeNamespace("Project.Data." + this.className);
            codeClass = new CodeTypeDeclaration("c" + this.className);
            codeClass.IsClass = true;
            codeClass.TypeAttributes = TypeAttributes.Public;
            codeClass.Comments.AddRange(AddComment(this.className));

            CodeAttributeDeclarationCollection collection = new CodeAttributeDeclarationCollection();
            collection.Add(new CodeAttributeDeclaration("Table", new CodeAttributeArgument(new CodePrimitiveExpression(this.tableName))));
            codeClass.CustomAttributes.AddRange(collection);
            nameSpace.Types.Add(codeClass);
            compileUnit.Namespaces.Add(nameSpace);
        }

        /// <summary>
        /// Add a constructor to the class.
        /// </summary>
        public void AddConstructors()
        {
            // Declare the constructor
            CodeConstructor constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;
            codeClass.Members.Add(constructor);

            CodeConstructor constructorWithParameters = new CodeConstructor();
            constructorWithParameters.Attributes = MemberAttributes.Public;
            constructorWithParameters.Comments.AddRange(AddComment("Vyhleda " + this.tableName + " podle ID"));
            constructorWithParameters.Comments.Add(new CodeCommentStatement("<param name=\"id\"></param>"));

            // Add parameters.
            constructorWithParameters.Parameters.Add(new CodeParameterDeclarationExpression(typeof(long), "id"));

            // Add method initialization logic
            CodeMethodReferenceExpression methodRef = new CodeMethodReferenceExpression(null, "Load");
            CodeVariableReferenceExpression var = new CodeVariableReferenceExpression("id");
            CodeMethodInvokeExpression invoke = new CodeMethodInvokeExpression(methodRef, var);
            constructorWithParameters.Statements.Add(invoke);
            codeClass.Members.Add(constructorWithParameters);
        }

        /// <summary>
        /// Adds a method to the class. This method multiplies values stored 
        /// in both fields.
        /// </summary>
        public void AddMethods()
        {
            MethodLoad();
            MethodInsert();
            MethodUpdate();
            MethodDelete();
        }

        /// <summary>
        /// Add properties to the class.
        /// </summary>
        public void AddProperties(DataTable table)
        {
            // Declare the property.
            CodeMemberProperty idProperty = new CodeMemberProperty();
            idProperty.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "PROPERTY"));
            idProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            idProperty.Name = "Id";
            idProperty.Comments.AddRange(AddComment("id " + this.tableName));

            if (table.Rows[0][table.Columns[3]].ToString() == "PRI")
                idProperty.CustomAttributes.Add(new CodeAttributeDeclaration("Key"));

            if (table.Rows[0][table.Columns[3]].ToString() == "MUL")
                idProperty.CustomAttributes.Add(new CodeAttributeDeclaration("Index"));

            idProperty.CustomAttributes.Add(new CodeAttributeDeclaration("Column", new CodeAttributeArgument(new CodePrimitiveExpression("id"))));
            idProperty.Type = new CodeTypeReference(typeof(long));
            idProperty.HasGet = true;
            idProperty.HasSet = true;
            idProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(null, "id")));
            idProperty.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(null, "id"), new CodePropertySetValueReferenceExpression()));
            codeClass.Members.Add(idProperty);

            CodeMemberField idField = new CodeMemberField();
            idField.Attributes = MemberAttributes.Family;
            idField.Name = "id";
            idField.Type = new CodeTypeReference(typeof(long));
            codeClass.Members.Add(idField);

            table.Rows.Remove(table.Rows[0]);

            foreach (DataRow row in table.Rows)
            {
                CodeMemberProperty property = new CodeMemberProperty();
                property.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                property.HasGet = true;
                property.HasSet = true;
                property.Comments.AddRange(AddComment(row[table.Columns[0]].ToString()));

                CodeMemberField field = new CodeMemberField();
                field.Attributes = MemberAttributes.FamilyAndAssembly;

                CodeMemberProperty typeProperty = new CodeMemberProperty();
                typeProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;
                typeProperty.HasGet = true;
                typeProperty.Type = new CodeTypeReference("cCodelistItem");

                foreach (DataColumn column in table.Columns)
                {
                    if (column.ColumnName == "Field")
                    {
                        property.Name = row[column].ToString().First().ToString().ToUpper() + row[column].ToString().Substring(1);
                        property.CustomAttributes.Add(new CodeAttributeDeclaration("Column", new CodeAttributeArgument(new CodePrimitiveExpression(row[column].ToString()))));
                        property.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(null, row[column].ToString())));
                        property.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(null, row[column].ToString()), new CodePropertySetValueReferenceExpression()));
                        field.Name = row[column].ToString();
                    }
                    else if (column.ColumnName == "Type")
                    {
                        if (row[column].ToString().StartsWith("int"))
                            property.Type = new CodeTypeReference(typeof(long));
                        else if (row[column].ToString().StartsWith("bigint"))
                            property.Type = new CodeTypeReference(typeof(UInt64));
                        else if (row[column].ToString().StartsWith("smallint"))
                            property.Type = new CodeTypeReference(typeof(Int16));
                        else if (row[column].ToString().StartsWith("tinyint"))
                            property.Type = new CodeTypeReference(typeof(bool));
                        else if (row[column].ToString().StartsWith("date"))
                            property.Type = new CodeTypeReference(typeof(DateTime));
                        else if (row[column].ToString().StartsWith("decimal"))
                            property.Type = new CodeTypeReference(typeof(decimal));
                        else if (row[column].ToString().StartsWith("varchar"))
                            property.Type = new CodeTypeReference(typeof(string));
                        else if (row[column].ToString().StartsWith("text"))
                            property.Type = new CodeTypeReference(typeof(string));
                        
                        field.Type = property.Type;
                    }
                    else if (column.ColumnName == "Key")
                    {
                        if (row[column].ToString() == "MUL")
                            property.CustomAttributes.Add(new CodeAttributeDeclaration("Index"));
                    }
                }

                if (row == table.Rows[row.Table.Rows.Count - 1])
                    field.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, ""));

                codeClass.Members.Add(property);
                codeClass.Members.Add(field);

                foreach (DataColumn column in table.Columns)
                {
                    if (IsCodeListItem(row[column].ToString()))
                    {
                        String[] splitList = row[column].ToString().Split('_');
                        string name = "";
                        foreach (String s in splitList)
                            name += s.First().ToString().ToUpper() + s.Substring(1);
                        typeProperty.Name = name;

                        string comment = splitList[0].First().ToString().ToUpper() + splitList[0].Substring(1);
                        for (int i = 1; i < splitList.Length; i++)
                            comment += " " + splitList[i];
                        typeProperty.Comments.AddRange(AddComment(comment));

                        CodeMethodReturnStatement returnStatement = new CodeMethodReturnStatement();
                        returnStatement.Expression =
                            new CodeMethodInvokeExpression(
                                new CodeTypeReferenceExpression("SCCodelists"),
                                "GetCodelistItem",
                                new CodeVariableReferenceExpression("(int)" + row[column].ToString())
                        );
                        typeProperty.GetStatements.Add(returnStatement);
                        codeClass.Members.Add(typeProperty);
                    }
                }
            }
        }

        private void MethodLoad()
        {
            // Declaring a method
            CodeMemberMethod method = new CodeMemberMethod();
            method.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "METHODS"));
            method.Attributes = MemberAttributes.FamilyAndAssembly;
            method.Name = "Load";
            method.ReturnType = new CodeTypeReference(typeof(bool));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(long), "id"));

            // Add field initialization logic
            CodeFieldReferenceExpression fieldRef = new CodeFieldReferenceExpression(null, "string sql");
            string sqlOutput = "$\"SELECT * FROM " + this.tableName + " WHERE";

            foreach (DataRow row in columnsOfTable.Rows)
                foreach (DataColumn column in columnsOfTable.Columns)
                    if (column.ToString() == "Field" && row[column].ToString() == "zruseno")
                        sqlOutput += " zruseno = 0 AND";

            sqlOutput += " id = {id}\"";
            method.Statements.Add(new CodeAssignStatement(fieldRef, new CodeArgumentReferenceExpression(sqlOutput)));

            // Declaring a return statement for method.
            CodeMethodReturnStatement returnStatement = new CodeMethodReturnStatement();
            returnStatement.Expression =
                new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("SCDatabases"),
                    "CreateObject",
                    new CodeThisReferenceExpression(),
                    new CodeVariableReferenceExpression("sql")
                );

            method.Statements.Add(returnStatement);
            codeClass.Members.Add(method);
        }

        private void MethodInsert()
        {
            // Declaring a method
            CodeMemberMethod method = new CodeMemberMethod();
            method.Attributes = MemberAttributes.FamilyAndAssembly;
            method.Name = "Insert";
            method.ReturnType = new CodeTypeReference(typeof(long));

            // Declaring a return statement for method.
            CodeMethodReturnStatement returnStatement = new CodeMethodReturnStatement();
            returnStatement.Expression =
                new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("SCDatabases"),
                    "InsertObject",
                    new CodeThisReferenceExpression(),
                    new CodeVariableReferenceExpression(String.Format("{0}" + this.tableName + "{0}", "\""))
                );

            method.Statements.Add(returnStatement);
            codeClass.Members.Add(method);
        }

        private void MethodUpdate()
        {
            // Declaring a method
            CodeMemberMethod method = new CodeMemberMethod();
            method.Attributes = MemberAttributes.FamilyAndAssembly;
            method.Name = "Update";
            method.ReturnType = new CodeTypeReference(typeof(bool));

            // Declaring a return statement for method.
            CodeMethodReturnStatement returnStatement = new CodeMethodReturnStatement();
            returnStatement.Expression =
                new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("SCDatabases"),
                    "UpdateObject",
                    new CodeThisReferenceExpression(),
                    new CodeVariableReferenceExpression(String.Format("{0}" + this.tableName + "{0}", "\"")),
                    new CodeVariableReferenceExpression("id")
                );

            method.Statements.Add(returnStatement);
            codeClass.Members.Add(method);
        }

        private void MethodDelete()
        {
            // Declaring a method
            CodeMemberMethod method = new CodeMemberMethod();
            method.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, ""));
            method.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            method.Name = "Delete";
            method.ReturnType = new CodeTypeReference(typeof(bool));

            // Add field initialization logic
            CodeConditionStatement conditionalStatement = new CodeConditionStatement(
                new CodeVariableReferenceExpression("id == 0"),
                new CodeStatement[] { new CodeMethodReturnStatement(new CodePrimitiveExpression(false)) }
                );
            method.Statements.Add(conditionalStatement);

            foreach (DataRow row in columnsOfTable.Rows)
                foreach (DataColumn column in columnsOfTable.Columns)
                    if (column.ToString() == "Field" && row[column].ToString() == "zrusil_id")
                    {
                        method.Statements.Add(new CodeAssignStatement(
                            new CodeFieldReferenceExpression(null, "zrusil_id"),
                            new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("SCUser"), "ID")));
                    }

            foreach (DataRow row in columnsOfTable.Rows)
                foreach (DataColumn column in columnsOfTable.Columns)
                    if (column.ToString() == "Field" && row[column].ToString() == "platnost_do")
                    {
                        method.Statements.Add(new CodeAssignStatement(
                            new CodeFieldReferenceExpression(null, "platnost_do"),
                            new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("DateTime"), "Now")));
                    }

            foreach (DataRow row in columnsOfTable.Rows)
                foreach (DataColumn column in columnsOfTable.Columns)
                    if (column.ToString() == "Field" && row[column].ToString() == "zruseno")
                    {
                        method.Statements.Add(new CodeAssignStatement(
                            new CodeFieldReferenceExpression(null, "zruseno"),
                            new CodePrimitiveExpression(true)));
                    }

            // Declaring a return statement for method.
            CodeMethodReturnStatement returnStatement = new CodeMethodReturnStatement();
            returnStatement.Expression =
                new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("SCDatabases"),
                    "UpdateObject",
                    new CodeThisReferenceExpression(),
                    new CodeVariableReferenceExpression(String.Format("{0}" + this.tableName + "{0}", "\"")),
                    new CodeVariableReferenceExpression("id")
                );

            method.Statements.Add(returnStatement);
            codeClass.Members.Add(method);
        }

        /// <summary>
        /// Generate CSharp source code from the compile unit.
        /// </summary>
        /// <param name="filename">Output file name</param>
        public void GenerateCSharpCode()
        {
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.BracingStyle = "C";
            options.VerbatimOrder = true;

            string directory = @"C:\Users\Acer\Desktop\Kozlovsky\projekty\DbEntityGenerator\Data\Generated objects\";
            string path = Path.Combine(directory, "c" + this.className + ".cs");

            using (StreamWriter sourceWriter = new StreamWriter(path))
            {
                provider.GenerateCodeFromCompileUnit(compileUnit, sourceWriter, options);
            }
        }

        private CodeCommentStatementCollection AddComment(string desc)
        {
            CodeCommentStatementCollection commentsCollection = new CodeCommentStatementCollection();
            CodeCommentStatement[] comments = {
                new CodeCommentStatement("<summary>"),
                new CodeCommentStatement(desc),
                new CodeCommentStatement("</summary>")
            };
            commentsCollection.AddRange(comments);
            return commentsCollection;
        }

        private bool IsCodeListItem(string column)
        {
            string[] columns = { "id", "name", "surname", "email" };

            if (columns.Contains(column))
                return true;

            return false;
        }
    }
}
