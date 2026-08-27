import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  Typography
} from '@mui/material';
import type { ReactNode } from 'react';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { EmptyState } from '@/components/common/EmptyState';

export interface Column<T> {
  key: string;
  header: string;
  render: (row: T) => ReactNode;
  align?: 'left' | 'right' | 'center';
  width?: number | string;
}

export interface AppTablePagination {
  /** Page courante 1-based */
  page: number;
  pageSize: number;
  total: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (pageSize: number) => void;
  pageSizeOptions?: number[];
}

interface Props<T> {
  columns: Column<T>[];
  rows: T[] | undefined;
  rowKey: (row: T) => string | number;
  isLoading?: boolean;
  emptyTitle?: string;
  emptyDescription?: string;
  onRowClick?: (row: T) => void;
  /** Hauteur max du conteneur scrollable. Active aussi le sticky header. Ex : `'60vh'` ou `'calc(100vh - 280px)'`. */
  maxHeight?: number | string;
  pagination?: AppTablePagination;
}

export function AppTable<T>({ columns, rows, rowKey, isLoading, emptyTitle, emptyDescription, onRowClick, maxHeight, pagination }: Props<T>) {
  if (isLoading) return <LoadingScreen />;
  if (!rows || rows.length === 0) {
    return (
      <Paper>
        <EmptyState title={emptyTitle ?? 'Aucune donnée'} description={emptyDescription} />
      </Paper>
    );
  }
  const scrollable = maxHeight !== undefined;
  return (
    <Paper sx={{ display: 'flex', flexDirection: 'column', maxHeight, overflow: 'hidden' }}>
      <TableContainer sx={{ flex: 1, maxHeight: scrollable ? '100%' : undefined, overflowY: scrollable ? 'auto' : 'visible' }}>
        <Table size="small" stickyHeader={scrollable}>
          <TableHead>
            <TableRow>
              {columns.map((c) => (
                <TableCell key={c.key} align={c.align ?? 'left'} sx={{ fontWeight: 600, width: c.width, bgcolor: 'background.paper' }}>
                  {c.header}
                </TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row) => (
              <TableRow
                key={rowKey(row)}
                hover
                sx={{ cursor: onRowClick ? 'pointer' : 'default' }}
                onClick={() => onRowClick?.(row)}
              >
                {columns.map((c) => (
                  <TableCell key={c.key} align={c.align ?? 'left'}>
                    {c.render(row)}
                  </TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
      {pagination ? (
        <TablePagination
          component="div"
          count={pagination.total}
          page={Math.max(0, pagination.page - 1)}
          onPageChange={(_, p) => pagination.onPageChange(p + 1)}
          rowsPerPage={pagination.pageSize}
          onRowsPerPageChange={(e) => pagination.onPageSizeChange?.(Number(e.target.value))}
          rowsPerPageOptions={pagination.pageSizeOptions ?? [25, 50, 100, 200]}
          labelRowsPerPage="Lignes par page"
          labelDisplayedRows={({ from, to, count }) => `${from}–${to} sur ${count}`}
          sx={{ borderTop: '1px solid', borderColor: 'divider' }}
        />
      ) : (
        <Box sx={{ p: 1, textAlign: 'right', borderTop: scrollable ? '1px solid' : 'none', borderColor: 'divider' }}>
          <Typography variant="caption" color="text.secondary">{rows.length} ligne(s)</Typography>
        </Box>
      )}
    </Paper>
  );
}
