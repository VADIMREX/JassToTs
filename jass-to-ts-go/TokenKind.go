package JassToTs

//const (
/** однострочный комментарий */
	const	LCOM = "lcom"
	/**
	 * многострочный комментарий (в JASS отсутствуют)
	 * @deprecated в JASS только однострочные комментарии начинающиеся с //
	 */
	//@Deprecated
	const	MCOM = "mcom"

	/** десятичное целое */
	const	NDEC = "ndec"
	/** восьмеричное целое */
	const	OCT = "oct"
	/** шеснадцатиричное целое в формате 0xNN */
	const	XHEX = "xhex"
	/** шеснадцатиричное целое в формате $NN */
	const	DHEX = "dhex"
	/** действительное */
	const	REAL = "real"
	/** шеснадцатиричное из 4х ASCII символов, записавыается в апостравах */
	const	ADEC = "adec"

	/** оператор */
	const	OPER = "oper"

	/** строка в кавычках (в JASS единственный тип строк) */
	const	DSTR = "dstr"
	/**
	 * строка в апострофах (, в JASS в апострафах хранятся целые, смотри <see cref="adec"/>)
	 * @deprecated не используется, в JASS единственный тип строк: заключённые в кавычках
	 */
	//@Deprecated
	const	SSTR = "sstr"

	/** идентификатор */
	const	NAME = "name"

	/** базовый тип */
	const	BTYP = "btyp"
	/** ключевое слово */
	const	KWD = "kwd"

	/** null значение */
	const	NULL = "null"
	/** булевое значение */
	const	BOOL = "bool"

	/** перевод строки */
	const	LN = "ln"

	/** левая скобка */
	const	LBRA = "lbra"
	/** правая скобка */
	const	RBRA = "rbra"
	/** левая квадратная скобка */
	const	LIND = "lind"
	/** правая квадратная скобка */
	const	RIND = "rind"

	/** макросы YDWE, пока что преобразуются в комментарий */
	const	YMACR = "ymacr"
//)