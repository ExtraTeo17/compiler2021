%namespace GardensPoint

%union
{
	public string val;
	public char type;
	public SyntaxTree syntaxTree;
	public List<SyntaxTree> syntaxTreeList;
	public Dictionary<string, SymbolInfo> symbolDict;
	public List<Declaration> declList;
	public List<string> idents;
}

%token Program OpenBracket CloseBracket Write Semicolon Eof Comma Hex
%token <val> IntNumber StringVar RealNumber Boolean TypeName Ident

%type <syntaxTree> instruction write_instruction
%type <syntaxTreeList> instructions
%type <symbolDict> declarations
%type <declList> declaration
%type <idents> identifiers

%%

program				: Program OpenBracket declarations instructions CloseBracket Eof
					{
						Compiler.syntaxTree = new Program($4);
						Compiler.symbolArray = new SymbolArray($3);
					}
					;

declarations		: declarations declaration
					{
						foreach (Declaration decl in $2)
						{
							$1.Add(decl.identifier, decl.symbolInfo);
						}
					}
					|
					{
						$$ = new Dictionary<string, SymbolInfo>();
					}
					;

declaration			: TypeName identifiers Semicolon
					{
						List<Declaration> declarations = new List<Declaration>();
						foreach (string ident in $2)
						{
							SymbolInfo info = new SymbolInfo($1);
							Declaration declaration = new Declaration(ident, info);
							declarations.Add(declaration);
						}
						$$ = declarations;
					}
					;
					
identifiers			: identifiers Comma Ident
					{
						$$.Add($3);
					}
					| Ident
					{
						$$ = new List<string>();
						$$.Add($1);
					}
					;

instructions		: instructions instruction
					{
						$1.Add($2);
					}
					|
					{
						$$ = new List<SyntaxTree>();
					}
					;

instruction			: write_instruction { }
					| exp_instruction { }
					;

exp_instruction		: exp Semicolon { }

exp					: 

write_instruction	: Write IntNumber Semicolon
					{
						$$ = new IntWriteInstruction(int.Parse($2));
					}
					| Write IntNumber Comma Hex Semicolon
					{
						$$ = new IntHexWriteInstruction(int.Parse($2));
					}
					| Write RealNumber Semicolon
					{
						$$ = new DoubleWriteInstruction(double.Parse($2));
					}
					| Write Boolean Semicolon
					{
						$$ = new BooleanWriteInstruction(bool.Parse($2));
					}
					| Write StringVar Semicolon
					{
						StringInfo info = Compiler.AddString($2);
						$$ = new StringWriteInstruction(info);
					}
					;

%%

public Parser(Scanner scanner) : base(scanner) { }
