BEGIN;

UPDATE auth.users
SET emailverified = true,
    updatedat = CURRENT_TIMESTAMP
WHERE COALESCE(emailverify, false) = true
  AND COALESCE(emailverified, false) = false;

CREATE OR REPLACE FUNCTION auth.fn_verify_email(
    p_token character varying,
    p_verify_time timestamp with time zone
) RETURNS boolean
LANGUAGE plpgsql
AS $$
DECLARE
    v_rows_affected integer;
BEGIN
    UPDATE auth.users
    SET emailverified = TRUE,
        emailVerify = TRUE,
        emailVerifyToken = NULL,
        emailVerifyExpiry = NULL,
        updatedat = CURRENT_TIMESTAMP
    WHERE emailVerifyToken = p_token
      AND emailVerifyExpiry > p_verify_time
      AND COALESCE(emailVerify, FALSE) = FALSE;

    GET DIAGNOSTICS v_rows_affected = ROW_COUNT;
    RETURN v_rows_affected > 0;
END;
$$;

COMMIT;
