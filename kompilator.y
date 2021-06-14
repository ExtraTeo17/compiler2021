%namespace GardensPoint

%union
{
	public string val;
	public SyntaxTree syntaxTree;
	public List<SyntaxTree> syntaxTreeList;
	public List<string> idents;
}

%token Program OpenBracket CloseBracket Read Write Semicolon Eof Comma Hex Assign LogicalSum LogicalProduct Equals NotEquals GreaterThan GreaterOrEqual LessThan LessOrEqual Plus Minus Multiplies Divides BitwiseSum BitwiseProduct BitwiseNegate LogicalNegate OpenPar ClosePar Return If Else While
%token <val> IntNumber IntHexNum StringVar RealNumber BoolValue Ident Int Double Bool

%type <val> typename
%type <syntaxTree> instruction read_instruction write_instruction loop_instruction if_else_instruction return_instruction block_instruction exp_instruction unary bitwise factor term relation logical assigner exp ident
%type <syntaxTreeList> declarations declaration instructions
%type <idents> identifiers

%%

program				: Program OpenBracket declarations instructions CloseBracket Eof
					{
						Compiler.syntaxTree = new Program($3, $4);
					}
					;

declarations		: declarations declaration
					{
						$1.AddRange($2);
					}
					|
					{
						$$ = new List<SyntaxTree>();
					}
					;

declaration			: typename identifiers Semicolon
					{
						List<SyntaxTree> declarations = new List<SyntaxTree>();
						foreach (string ident in $2)
						{
							Declaration declaration = new Declaration(ident, $1);
							declarations.Add(declaration);
						}
						$$ = declarations;
					}
					;
					
typename			: Int { }
					| Double { }
					| Bool { }
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
					| read_instruction { }
					| exp_instruction { }
					| block_instruction { }
					| return_instruction { }
					| if_else_instruction { }
					| loop_instruction { }
					;

read_instruction	: Read ident Semicolon
					{
						$$ = new ReadInstruction($2);
					}
					| Read ident Comma Hex Semicolon
					{
						$$ = new HexReadInstruction($2);
					}
					;

if_else_instruction	: If OpenPar exp ClosePar instruction
					{
						$$ = new ConditionalInstruction($3, $5);
					}
					| If OpenPar exp ClosePar instruction Else instruction
					{
						$$ = new ConditionalInstruction($3, $5, $7);
					}
					;

loop_instruction	: While OpenPar exp ClosePar instruction
					{
						$$ = new LoopInstruction($3, $5);
					}
					;

return_instruction	: Return Semicolon
					{
						$$ = new ReturnInstruction();
					}
					;

block_instruction	: OpenBracket instructions CloseBracket
					{
						$$ = new BlockInstruction($2);
					}
					;

exp_instruction		: exp Semicolon { }
					;

exp					: ident Assign exp
					{
						$$ = new AssignOperation($1, $3);
					}
					| assigner { }
					;

assigner			: assigner LogicalSum logical
					{
						$$ = new LogicalSumOperation($1, $3);
					}
					| assigner LogicalProduct logical
					{
						$$ = new LogicalProductOperation($1, $3);
					}
					| logical { }
					;

logical				: logical Equals relation
					{
						$$ = new EqualsOperation($1, $3);
					}
					| logical NotEquals relation
					{
						$$ = new NotEqualsOperation($1, $3);
					}
					| logical GreaterThan relation
					{
						$$ = new GreaterThanOperation($1, $3);
					}
					| logical GreaterOrEqual relation
					{
						$$ = new GreaterOrEqualOperation($1, $3);
					}
					| logical LessThan relation
					{
						$$ = new LessThanOperation($1, $3);
					}
					| logical LessOrEqual relation
					{
						$$ = new LessOrEqualOperation($1, $3);
					}
					| relation { }
					;

relation			: relation Plus term
					{
						$$ = new AdditionOperation($1, $3);
					}
					| relation Minus term
					{
						$$ = new SubstractionOperation($1, $3);
					}
					| term { }
					;

term				: term Multiplies factor
					{
						$$ = new MultiplicationOperation($1, $3);
					}
					| term Divides factor
					{
						$$ = new DivisionOperation($1, $3);
					}
					| factor { }
					;

factor				: factor BitwiseSum bitwise
					{
						$$ = new BitwiseSumOperation($1, $3);
					}
					| factor BitwiseProduct bitwise
					{
						$$ = new BitwiseProductOperation($1, $3);
					}
					| bitwise { }
					;

bitwise				: Minus bitwise
					{
						$$ = new UnaryMinusOperation($2);
					}
					| BitwiseNegate bitwise
					{
						$$ = new BitwiseNegateOperation($2);
					}
					| LogicalNegate bitwise
					{
						$$ = new LogicalNegateOperation($2);
					}
					| OpenPar Int ClosePar bitwise
					{
						$$ = new ConvertToIntOperation($4);
					}
					| OpenPar Double ClosePar bitwise
					{
						$$ = new ConvertToDoubleOperation($4);
					}
					| unary { }
					;

unary				: OpenPar exp ClosePar
					{
						$$ = $2;
					}
					| IntNumber
					{
						$$ = new IntNumber(int.Parse($1));
					}
					| IntHexNum
					{
						$$ = new IntNumber(Convert.ToInt32($1, 16));
					}
					| RealNumber
					{
						$$ = new RealNumber(double.Parse($1));
					}
					| BoolValue
					{
						$$ = new BoolValue(bool.Parse($1));
					}
					| ident { }
					;

ident				: Ident
					{
						$$ = new Identifier($1);
					}
					;

write_instruction	: Write exp Semicolon
					{
						$$ = new WriteInstruction($2);
					}
					| Write exp Comma Hex Semicolon
					{
						$$ = new HexWriteInstruction($2);
					}
					| Write StringVar Semicolon
					{
						StringInfo info = Compiler.AddString($2);
						$$ = new StringWriteInstruction(info);
					}
					;

%%

public Parser(Scanner scanner) : base(scanner) { }
