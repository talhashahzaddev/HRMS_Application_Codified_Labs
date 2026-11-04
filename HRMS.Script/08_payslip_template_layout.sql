-- Payslip layout catalog and layoutKey on organization templates (Phase 2)
-- Safe to run on existing databases.

CREATE TABLE IF NOT EXISTS payroll.paysliplayoutcatalog (
    layoutkey         varchar(50) PRIMARY KEY,
    displayname       varchar(100) NOT NULL,
    description       text,
    previewimageurl   varchar(500),
    configschema      jsonb NOT NULL DEFAULT '{}'::jsonb,
    defaultconfig     jsonb NOT NULL DEFAULT '{}'::jsonb,
    rendererkey       varchar(100) NOT NULL,
    isactive          boolean NOT NULL DEFAULT true,
    sortorder         int NOT NULL DEFAULT 0,
    createdat         timestamptz DEFAULT CURRENT_TIMESTAMP,
    updatedat         timestamptz DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO payroll.paysliplayoutcatalog (layoutkey, displayname, description, rendererkey, sortorder)
VALUES
    ('classic_stacked', 'Classic stacked', 'Traditional payslip with stacked sections: employee details, earnings, deductions, and net pay.', 'classic_stacked', 1),
    ('split_columns', 'Split columns', 'Side-by-side earnings and deductions columns for easier comparison.', 'split_columns', 2),
    ('compact_corporate', 'Compact corporate', 'Summary strip with gross, deductions, and net pay plus compact line items.', 'compact_corporate', 3)
ON CONFLICT (layoutkey) DO NOTHING;

ALTER TABLE payroll.paysliptemplates
    ADD COLUMN IF NOT EXISTS layoutkey varchar(50) NOT NULL DEFAULT 'classic_stacked',
    ADD COLUMN IF NOT EXISTS layoutconfig jsonb NOT NULL DEFAULT '{}'::jsonb;

UPDATE payroll.paysliptemplates
SET layoutkey = 'classic_stacked'
WHERE layoutkey IS NULL OR layoutkey = '';

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_paysliptemplates_layout'
    ) THEN
        ALTER TABLE payroll.paysliptemplates
            ADD CONSTRAINT fk_paysliptemplates_layout
            FOREIGN KEY (layoutkey)
            REFERENCES payroll.paysliplayoutcatalog(layoutkey);
    END IF;
END $$;
