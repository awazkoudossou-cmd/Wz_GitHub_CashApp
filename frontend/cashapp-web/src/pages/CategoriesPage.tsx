import {
  Autocomplete,
  Box,
  Button,
  Card,
  CardContent,
  Grid,
  IconButton,
  MenuItem,
  Paper,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Typography
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import UploadFileIcon from '@mui/icons-material/UploadFile';
import { Fragment, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { PageContainer } from '@/components/layout/PageContainer';
import { PageHeader } from '@/components/common/PageHeader';
import { LoadingScreen } from '@/components/common/LoadingScreen';
import { EmptyState } from '@/components/common/EmptyState';
import {
  useCategories,
  useCreateCategory,
  useUpdateCategoryStatus
} from '@/modules/categories/hooks';
import { useCategoryGroups } from '@/modules/category-groups/hooks';
import { useIsFeatureEnabled } from '@/hooks/useFeatures';
import { OperationDirection, FeatureCodes } from '@/types/enums';
import type { CategoryListItem } from '@/types';
import { StatusChip } from '@/components/common/StatusChip';

const UNGROUPED_LABEL = 'Sans groupe';

export function CategoriesPage() {
  const navigate = useNavigate();
  const importsEnabled = useIsFeatureEnabled(FeatureCodes.ADV_IMPORTS);
  const { data, isLoading } = useCategories();
  const groups = useCategoryGroups();
  const updateStatus = useUpdateCategoryStatus();
  const create = useCreateCategory();

  const [open, setOpen] = useState(false);
  const [code, setCode] = useState('');
  const [label, setLabel] = useState('');
  const [direction, setDirection] = useState<OperationDirection>(OperationDirection.IN);
  const [groupName, setGroupName] = useState('');
  const [collapsed, setCollapsed] = useState<Record<string, boolean>>({});
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const submit = async () => {
    if (!groupName.trim()) return;
    await create.mutateAsync({ code: code.trim().toUpperCase(), label: label.trim(), direction, groupName: groupName.trim() });
    setCode(''); setLabel(''); setGroupName(''); setOpen(false);
  };

  const rows = data ?? [];

  // Tri global (groupe puis code) pour un découpage de page cohérent avec le regroupement affiché.
  const sortedRows = useMemo(() => {
    return [...rows].sort((a, b) => {
      const ga = a.groupName ?? UNGROUPED_LABEL;
      const gb = b.groupName ?? UNGROUPED_LABEL;
      return ga !== gb ? ga.localeCompare(gb) : a.code.localeCompare(b.code);
    });
  }, [rows]);

  const pageItems = useMemo(
    () => sortedRows.slice((page - 1) * pageSize, page * pageSize),
    [sortedRows, page, pageSize]
  );

  // Organise les catégories de la page courante par groupe (sections repliables).
  const grouped = useMemo(() => {
    const map = new Map<string, CategoryListItem[]>();
    for (const r of pageItems) {
      const key = r.groupName ?? UNGROUPED_LABEL;
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(r);
    }
    return Array.from(map.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([key, items]) => ({ key, items }));
  }, [pageItems]);

  const groupOptions = (groups.data ?? []).map((g) => g.name);

  return (
    <PageContainer maxWidth={false}>
      <Box sx={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 112px)' }}>
      <PageHeader
        title="Catégories"
        actions={
          <Stack direction="row" spacing={1}>
            {importsEnabled && (
              <Button variant="outlined" startIcon={<UploadFileIcon />} onClick={() => navigate('/imports?type=CATEGORIES')}>
                Importer
              </Button>
            )}
            <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen((o) => !o)}>{open ? 'Fermer' : 'Nouvelle catégorie'}</Button>
          </Stack>
        }
      />
      {open && (
        <Card sx={{ mb: 2 }}>
          <CardContent>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={2}><TextField fullWidth size="small" label="Code" value={code} onChange={(e) => setCode(e.target.value)} /></Grid>
              <Grid item xs={12} sm={3}><TextField fullWidth size="small" label="Libellé" value={label} onChange={(e) => setLabel(e.target.value)} /></Grid>
              <Grid item xs={12} sm={2}>
                <TextField select fullWidth size="small" label="Direction" value={direction} onChange={(e) => setDirection(e.target.value as OperationDirection)}>
                  <MenuItem value={OperationDirection.IN}>IN (entrée)</MenuItem>
                  <MenuItem value={OperationDirection.OUT}>OUT (sortie)</MenuItem>
                </TextField>
              </Grid>
              <Grid item xs={12} sm={4}>
                <Autocomplete
                  freeSolo
                  fullWidth
                  size="small"
                  options={groupOptions}
                  value={groupName}
                  onInputChange={(_, value) => setGroupName(value)}
                  renderInput={(params) => (
                    <TextField {...params} label="Groupe" required
                      helperText="Sélectionne un groupe existant ou saisis-en un nouveau." />
                  )}
                />
              </Grid>
              <Grid item xs={12} sm={1}>
                <Stack alignItems="center" justifyContent="center" sx={{ height: '100%' }}>
                  <Button variant="contained" onClick={submit} disabled={!code.trim() || !label.trim() || !groupName.trim()}>OK</Button>
                </Stack>
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      )}

      {isLoading ? <LoadingScreen /> : rows.length === 0 ? (
        <Paper><EmptyState title="Aucune catégorie" /></Paper>
      ) : (
        <Paper sx={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, overflow: 'hidden' }}>
          <TableContainer sx={{ flex: 1, maxHeight: '100%', overflowY: 'auto' }}>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Code</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Libellé</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Direction</TableCell>
                  <TableCell sx={{ fontWeight: 600, bgcolor: 'background.paper' }}>Statut</TableCell>
                  <TableCell align="right" sx={{ fontWeight: 600, bgcolor: 'background.paper' }}></TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {grouped.map((g) => {
                  const isCollapsed = !!collapsed[g.key];
                  return (
                    <Fragment key={g.key}>
                      <TableRow sx={{ bgcolor: 'action.hover' }}>
                        <TableCell colSpan={5} sx={{ py: 0.5 }}>
                          <Stack direction="row" alignItems="center" spacing={1}>
                            <IconButton size="small" onClick={() => setCollapsed((c) => ({ ...c, [g.key]: !c[g.key] }))}>
                              {isCollapsed ? <ChevronRightIcon fontSize="small" /> : <ExpandMoreIcon fontSize="small" />}
                            </IconButton>
                            <Typography variant="body2" fontWeight={600}>{g.key}</Typography>
                            <Typography variant="caption" color="text.secondary">({g.items.length})</Typography>
                          </Stack>
                        </TableCell>
                      </TableRow>
                      {!isCollapsed && g.items.map((r) => (
                        <TableRow key={r.id} hover>
                          <TableCell>{r.code}</TableCell>
                          <TableCell>{r.label}</TableCell>
                          <TableCell><StatusChip status={r.direction} variant="direction" /></TableCell>
                          <TableCell><StatusChip status={String(r.isActive)} variant="active" /></TableCell>
                          <TableCell align="right">
                            <Switch size="small" checked={r.isActive}
                              onChange={(_, v) => updateStatus.mutate({ id: r.id, isActive: v })} />
                          </TableCell>
                        </TableRow>
                      ))}
                    </Fragment>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
          <TablePagination
            component="div"
            count={sortedRows.length}
            page={page - 1}
            onPageChange={(_, p) => setPage(p + 1)}
            rowsPerPage={pageSize}
            onRowsPerPageChange={(e) => { setPageSize(Number(e.target.value)); setPage(1); }}
            rowsPerPageOptions={[25, 50, 100]}
            labelRowsPerPage="Lignes par page"
            labelDisplayedRows={({ from, to, count }) => `${from}–${to} sur ${count}`}
            sx={{ borderTop: '1px solid', borderColor: 'divider' }}
          />
        </Paper>
      )}
      </Box>
    </PageContainer>
  );
}
