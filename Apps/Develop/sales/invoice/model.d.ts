﻿

export interface TAgent extends IElement {
	Id: number,
	Name: string,
	Memo: string
}

export interface TRow extends IElement {
	Qty: number;
	Price: number;
	Sum: number;
}

export interface TRows extends IElementArray<TRow> {
}

export interface TDocument extends IElement {
	Id: number;
	Date: Date;
	Done: boolean;
	Memo: string;
	Sum: number;
	Agent: TAgent;
	Rows: TRows;
}

export interface TRoot extends IRoot {
	Document: TDocument;
}