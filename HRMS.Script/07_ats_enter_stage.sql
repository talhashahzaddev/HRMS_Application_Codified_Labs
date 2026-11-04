-- ============================================================
-- Migration 07: ATS Inbox - "Enter to Stage" Column
-- ============================================================
-- Adds enteredtostage flag to jobs.jobapplications.
-- Applications start with enteredtostage = false (ATS Inbox).
-- Managers review them in the ATS tab and click "Enter to Stage"
-- to flip it to true, making them appear in the Kanban boards.
-- ============================================================

ALTER TABLE jobs.jobapplications
ADD COLUMN IF NOT EXISTS enteredtostage BOOLEAN NOT NULL DEFAULT FALSE;

COMMENT ON COLUMN jobs.jobapplications.enteredtostage IS
    'Whether this application has been reviewed in the ATS Inbox and promoted to the formal hiring pipeline. 
     FALSE = still in ATS inbox (not visible in kanban boards). 
     TRUE = entered into hiring stages (visible in kanban boards).';

-- Create an index for the filter queries
CREATE INDEX IF NOT EXISTS idx_jobapplications_enteredtostage
    ON jobs.jobapplications (organizationid, enteredtostage);
