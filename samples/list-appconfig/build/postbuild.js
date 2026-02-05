import { chmod } from 'fs/promises';

await chmod('dist/index.js', 0o755);
