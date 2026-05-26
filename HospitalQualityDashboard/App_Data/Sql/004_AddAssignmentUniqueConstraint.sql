IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE name = N'UQ_PhanCongChiSo_ChiSo_KhoaPhong'
      AND parent_object_id = OBJECT_ID(N'dbo.PhanCongChiSo')
)
BEGIN
    IF OBJECT_ID('tempdb..#DuplicatePhanCongChiSo') IS NOT NULL
        DROP TABLE #DuplicatePhanCongChiSo;

    ;WITH Ranked AS (
        SELECT
            PhanCongChiSoId,
            ChiSoChatLuongId,
            KhoaPhongId,
            FIRST_VALUE(PhanCongChiSoId) OVER (
                PARTITION BY ChiSoChatLuongId, KhoaPhongId
                ORDER BY DangHoatDong DESC, PhanCongChiSoId
            ) AS KeepPhanCongChiSoId,
            ROW_NUMBER() OVER (
                PARTITION BY ChiSoChatLuongId, KhoaPhongId
                ORDER BY DangHoatDong DESC, PhanCongChiSoId
            ) AS RowNumber
        FROM dbo.PhanCongChiSo
    )
    SELECT PhanCongChiSoId, KeepPhanCongChiSoId
    INTO #DuplicatePhanCongChiSo
    FROM Ranked
    WHERE RowNumber > 1;

    UPDATE bc
    SET PhanCongChiSoId = dup.KeepPhanCongChiSoId
    FROM dbo.BaoCao bc
    INNER JOIN #DuplicatePhanCongChiSo dup ON dup.PhanCongChiSoId = bc.PhanCongChiSoId;

    DELETE pc
    FROM dbo.PhanCongChiSo pc
    INNER JOIN #DuplicatePhanCongChiSo dup ON dup.PhanCongChiSoId = pc.PhanCongChiSoId;

    DROP TABLE #DuplicatePhanCongChiSo;

    UPDATE bc
    SET PhanCongChiSoId = pc.PhanCongChiSoId
    FROM dbo.BaoCao bc
    INNER JOIN dbo.PhanCongChiSo pc
        ON pc.KhoaPhongId = bc.KhoaPhongId
       AND pc.ChiSoChatLuongId = bc.ChiSoChatLuongId
    WHERE bc.PhanCongChiSoId IS NULL;

    ALTER TABLE dbo.PhanCongChiSo
    ADD CONSTRAINT UQ_PhanCongChiSo_ChiSo_KhoaPhong UNIQUE (ChiSoChatLuongId, KhoaPhongId);
END
